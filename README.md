# looking-for-a-correlation-between-blood-sugar-levels-and-skin-conductivity

Yes. I have diabetes. Therefore, regular monitoring of blood sugar levels is necessary. 
This project was created as an attempt to obtain blood sugar value using a non-invasive method.
Discovering a correlation itself takes a long time, so the project goes public to attract supporters.

### Let's start
Analog Devices released a very interesting chip [AD5933](http://www.analog.com/AD5933?doc=AD5933.pdf).
In my opinion, this is a very promising tool for this purpose. 
Prepare the hardware according to the datasheet.
PCB in lay format [bio.lay6](/assets/bio.lay6).
It should look something like this 

![](/assets/AD5933.jpg)

resistor 200 kom, this is where the external elements end.

The implementation of the i2c interface for the computer is made using a ready-made module on the CP2112 chip.

![](/assets/CP2112.jpg)

It turned out something like

![](/assets/result.jpg)

Textile elastic band with velcro, scanning electrodes made of nickel plate /* better gold ones, but haven’t found them yet) */

> [!CAUTION]
> a few horror stories: don’t forget safety precautions!!!
> power supplies for desktop computers have the following circuit at the input
> 
> ![](/assets/power.jpg)
>
> if the ground wire is damaged at the junction of SU01 and SU02, half the supply voltage appears. And this point is connected to the computer case.
> 
> **Hence the rule: when touching the computer, do not touch grounded objects, such as a radiator!**
> **However, this rule applies to all devices that are plugged into an outlet.**

### some pictures
![](/assets/startM.jpg) ![](/assets/connectedM.jpg)

The scanning voltage can be 200 mV, 400 mV, 1 V, 2 V.

![](/assets/rs.jpg)

![](/assets/rgb.jpg)

![](/assets/rbg.jpg)

We are looking for a correlation, don’t forget to share the result. A successful experiment will aim to create a portable, self-contained device for continuous monitoring. Naturally, the project will also be open source.
