using OxyPlot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SLAB_HID_TO_SMBUS;

namespace Bio
{
    internal class MainWindowBL
    {
        
        public event EventHandler<DataEvent> NewDataBlockReady;
        public event EventHandler ProcessEnd;
        public event EventHandler<DataEvent> ProgressNow;

        private const ushort vid = 0x10C4;
        private const ushort pid = 0xEA90;
        private IntPtr connectedDevice;
        private const byte AD5933SlaveAddress = 0x1A;

        private bool _isRun;
        private bool _isDeviceConnected;
        private uint _startFrq;
        private uint _endFrq;
        private uint _frqStep;
        private byte _voltage;
        private ICalculator _calculator;
        private IFilter _filter;

        public bool InitI2C()
        {
            uint numDevices = 0;
            _ = CP2112_DLL.HidSmbus_GetNumDevices(ref numDevices, vid, pid);
            _isDeviceConnected = numDevices == 1;
            bool result = _isDeviceConnected;
            return result;
        }

        public void Start(uint startFrq, uint endFrq, uint frqStep, byte voltage , uint typeOfScan)
        {
            if (_isDeviceConnected & _isRun == false)
            {
                _startFrq = startFrq;
                _endFrq = endFrq;
                _frqStep = frqStep;
                _voltage = voltage;
                _isRun = true;
                _calculator = ( typeOfScan == 1) ? new Phase() : new Resistance();
                _filter = new Median();
                Run();
            }
        }

        internal void Run()
        {
            Task _testTask = new(new Action(() =>
            {

                OnProgressNow(0);

                _ = CP2112_DLL.HidSmbus_Open(ref connectedDevice, 0, vid, pid);

                _ = CP2112_DLL.HidSmbus_SetSmbusConfig(connectedDevice, 100000, 0x02, 0, 100, 100, 0, 1);

                _ = CP2112_DLL.HidSmbus_SetGpioConfig(connectedDevice, 0, 0, 6, 0);

                uint stepCount = (_endFrq - _startFrq) / _frqStep;
                uint stepProgress = stepCount;
                uint stepCounter = 0;

                for (uint reminder = 511; reminder >= 2; reminder--)
                {
                    if (stepCount % reminder == 0)
                    {
                        stepCount = reminder;
                        break;
                    }
                }

                Write(0x80, new byte[] { (byte)(0xB0 | _voltage), 0x80 });
                Write(0x8A, new byte[] { 0x00, 0x7F });

                for (uint startFrq = _startFrq; startFrq < _endFrq; startFrq += stepCount * _frqStep)
                {

                    bool complete = false;
                    int counter = 0;
                    byte[] res = new byte[3];
                    List<DataPoint> collectedData = new();

                    Write(0x80, new byte[] { (byte)(0xB0 | _voltage) });

                    SetFrq(startFrq, 0x82);
                    SetFrq(_frqStep, 0x85);
                    SetStep(((startFrq + (stepCount * _frqStep)) < _endFrq) ? stepCount : stepCount + 1, 0x88);

                    Write(0x80, new byte[] { 0x10 });

                    if (_isRun == false) { break; }

                    Write(0x80, new byte[] { (byte)(0x20 | _voltage) });
                    do
                    {
                        do
                        {
                            res = Read(0x8f, 1);
                            complete = (res[0] & 0x04) != 0;
                        } while (((res[0] & 0x02) == 0) & (complete == false));

                        res = Read(0x94, 2);
                        short real = res[0];
                        real <<= 8;
                        real |= res[1];
                        res = Read(0x96, 2);
                        short img = res[0];
                        img <<= 8;
                        img |= res[1];


                        double iimg = img;
                        double ireal = real;

                        (bool, double) filtred = _filter.Calculate(_calculator.Calculate(ireal, iimg));
                        if (filtred.Item1 == false)
                        {
                            Write(0x80, new byte[] { (byte)(0x40 | _voltage) });
                        }
                        else
                        {
                            collectedData.Add(new DataPoint((startFrq + counter * _frqStep) / 10, filtred.Item2));
                            counter++;
                            stepCounter++;
                            OnProgressNow((int)(stepCounter * 100 / stepProgress));
                            Write(0x80, new byte[] { (byte)(0x30 | _voltage) });
                        }
                    } while (complete == false & _isRun);
                    Write(0x80, new byte[] { (byte)(0xA0 | _voltage) });

                    OnNewDataBlock(collectedData, (int)((startFrq + stepCount * _frqStep) * 100 / _endFrq));
                }
                OnProcessEnd();
                _isRun = false;
                int opened = 0;
                _ = CP2112_DLL.HidSmbus_IsOpened(connectedDevice, ref opened);
                if (opened == 1)
                {
                    _ = CP2112_DLL.HidSmbus_Close(connectedDevice);
                }
            }));
            _testTask.Start();
        }

        public void Stop()
        {
            _isRun = false;
        }

        internal void OnNewDataBlock(List<DataPoint> data, int progress)
        {
            DataEvent e = new()
            {
                data = data,
                Progress = progress
            };
            NewDataBlockReady?.Invoke(this, e);
        }

        internal void OnProgressNow( int progress)
        {
            DataEvent e = new()
            {
                data = null,
                Progress = progress
            };
            ProgressNow?.Invoke(this, e);
        }

        internal void OnProcessEnd()
        {
            Task _testTask = new(new Action(() =>
            {
                ProcessEnd?.Invoke(this, EventArgs.Empty);
            }));
            _testTask.Start();
        }

        private void SetFrq(uint frq, byte addr)
        {
            float fl = (float)((float)frq / 10) / (16776000 / 4) * 0x8000000;
            int f = (int)fl;
            byte[] set = new byte[3] { (byte)(f >> 16), (byte)(f >> 8), (byte)(f) };
            Write(addr, set);
        }

        private void SetStep(uint step, byte addr)
        {
            byte[] set = new byte[2] { (byte)(step >> 8), (byte)(step) };
            Write(addr, set);

        }

        private void Write(byte addr, byte[] data)
        {
            byte len = (byte)data.Length;
            if (len == 1)
            {
                byte[] send = new byte[len + 1];
                send[0] = addr;
                send[1] = data[0];
                _ = CP2112_DLL.HidSmbus_WriteRequest(connectedDevice, AD5933SlaveAddress, send, 2);
            }
            else
            {
                _ = CP2112_DLL.HidSmbus_WriteRequest(connectedDevice, AD5933SlaveAddress, new byte[] { 0xB0, addr}, 2);
                byte[] send = new byte[len + 2];
                data.CopyTo(send, 2);
                send[0] = 0xA0;
                send[1] = len;
                _ = CP2112_DLL.HidSmbus_WriteRequest(connectedDevice, AD5933SlaveAddress, send, (byte)(len + 2));
            }
        }

        private byte[] Read(byte adr, byte count)
        {
            byte status = 0;
            byte[] readbuff = new byte[61];
            byte[] data = new byte[count];
            byte bytesRead = 0;

            try
            {
                for (byte totBytesRead = 0; totBytesRead < count;)
                {

                    _ = CP2112_DLL.HidSmbus_AddressReadRequest(connectedDevice, AD5933SlaveAddress, 1, 1, new byte[] { adr });
                    _ = CP2112_DLL.HidSmbus_ForceReadResponse(connectedDevice, 1);
                    _ = CP2112_DLL.HidSmbus_GetReadResponse(connectedDevice, ref status, readbuff, 61, ref bytesRead);
                    data[totBytesRead] = readbuff[0];
                    totBytesRead++;
                    adr++;
                }
            }
            catch
            {
            }
            return data;
        }

    }
}
