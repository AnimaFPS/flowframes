# Flowframes - Windows GUI for Video Interpolation
Flowframes Windows GUI for video interpolation - Supports DAIN NCNN as well as RIFE Pytorch and NCNN implementations.


![img](/Media/flowframes_gui.png)

## Installation

* Download the latest [release](https://github.com/AnimaFPS/flowframes/releases/latest)
* Run Flowframes.exe

or build for source

## Configuration

All Settings have reasonable defaults, so users do not need to do any configuration before using the program.

Here is an explanation of some of the more important settings.

### General

* Maximum Video Size: Frames are exported at this resolution if the video is larger. Lower resolutions speed up interpolation a lot.

### Interpolation

* Copy Audio: Audio will be saved to a separate file when extracting the frames and will afterwards be merged into the output.
  * Not guaranteed to work with all audio codecs. Supported are: M4A/AAC, Vorbis, Opus, MP2, PCM/Raw.
* Remove Duplicate Frames: This is meant for 2D animation. Removing duplicates makes a smooth interpolation possible.
  * You can disable this completely if you only use content without duplicates (e.g. camera footage, CG renders).
* Animation Loop: This will make looped animations interpolate to a perfect loop by copying the first frame to the end of the frames.
* Don't Interpolate Scene Changes: This avoids interpolating scene changes (cuts) as this would produce weird a morphing effect.
* Auto-Encode: Encode video while interpolating. Optionally delete the already encoded frames to minimize disk space usage.
* Save Output Frames As JPEG: Save interpolated frames as JPEG before encoding. Not recommended unless you have little disk space.

### AI Specific Settings

* RIFE - UHD Mode - This mode changes some scaling parameters and should improve results on high-resolution video.
* GPU IDs: `0` is the default for setups with one dedicated GPU. Four dedicated GPUs would mean `0,1,2,3` for example.
* NCNN Processing Threads: Increasing this number to 2, 3 or 4 can improve GPU utilization, but also slow things down.

### Video Export

* Encoding Options: Set options for video/GIF encoding. Refer to the **FFmpeg** documentation for details.
* Minimum Video Length: Make sure the output is as long as this value by looping it.
* Maximum Output Frame Rate: Limit frame rate by downsampling, for example, if you want a 60 FPS output from a 24 FPS video.

### Debugging / Experimental

* Show Hidden CMD Windows: This will show the windows for AI processes. Can be useful for debugging.
* FFprobe: Count Frames Manually: This uses a slower way of getting the input video's total frame count, but works more reliably. 



## System Requirements

#### Minimum: 

* Vulkan-capable GPU (Nvidia Kepler or newer, AMD GCN 2 or newer)

#### Recommended: 

* Modern CUDA-capable GPU (Nvidia Maxwell or newer) with 6 GB VRAM or more
* 16 GB RAM
* Modern CPU (Intel Core 7xxx Series or newer, AMD Ryzen Series)



## Frequently Asked Questions (FAQ)

**Q:** What's the difference between RIFE CUDA and RIFE NCNN? Which one should I use?  
**A:** The results should be identical, however, RIFE-NCNN also runs on AMD cards, CUDA only on Nvidia. If you have an Nvidia card, use CUDA as it's faster.

**Q:** What is frame de-duplication for? When should I enable or disable it?  
**A:** It's primarily for 2D animation, where the video has consecutive frames without changes. These have to be removed before interpolation to avoid choppy outputs. Enable it for 2D animation, disable it for constant frame rate content like camera footage or 3D rendered videos.

**Q:** My output looks very choppy, especially in dark (or low-contrast) scenes!  
**A:** Disable De-Duplication (or reduce the threshold if you still need it)

**Q:** What's the technical difference between the de-duplication modes "Remove During Extraction" and "Remove After Extraction"?  
**A:** "During" uses ffmpeg's `mpdecimate` filter and won't extract duplicate frames at all. "After" extracts all frames and *then* checks for duplicates by checking the image difference using Magick.NET, which is slower but more accurate and flexible.

**Q:** How does Auto-Encode work, and should I enable or disable it?  
**A:** It encodes your output video during interpolation, instead of afterwards. Enable it unless you have a very weak CPU.

**Q:** I downloaded a "Full" package but now want to switch to my own system Python installation. How do I do that?  
**A:** Go to `FlowframesData/pkgs/` and delete the folders `py-tu` or `py-amp`, whichever you have. Flowframes will now try to use system python.
