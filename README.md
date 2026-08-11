# Camera Dino

A local application for video monitoring and restreaming. It captures camera streams via network protocols (such as RTSP and ONVIF) and converts them in real-time for optimized streaming and viewing.

## How it works

**Camera Dino** acts essentially as a **Video Restreaming/Transcoding Server**. It utilizes powerful media engines (**go2rtc** and **FFmpeg**) embedded in a fast, local solution.

It works in the following stages:
1. **Capture (Ingress):** It connects to a video source (such as an IP Camera, DVR, or NVR) by pulling the original network stream. Typically, these sources transmit using protocols like **RTSP** or **ONVIF**, which are too heavy or unsupported for native web browsers.
2. **Processing (Transmuxing):** Running in the background, the program intercepts these raw network packets. Instead of just saving the video, the engine performs a "simultaneous translation" of that protocol, repackaging the video and audio in milliseconds with ultra-low latency.
3. **Transmission (Egress):** It takes this processed stream and "pushes it back onto the network" under new, much more modern and accessible protocols, such as **WebRTC** or **MSE**. 

If you have a security camera with a complicated RTSP link, Camera Dino acts as a bridge: it sucks in the RTSP and spits out a lightweight web interface that any device on your network (PC, mobile, Smart TV) can open instantly.

## Build and Compilation

To recompile the project, run the PowerShell script `build_inno.ps1` in the root directory.
The icons can be generated using the `Make-ValidIcon.ps1` and `create_icon.ps1` scripts.

The generated installer will be saved in the `Release` folder.
