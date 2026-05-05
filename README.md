# Creanex Data Visualization app

The app visualizes gaze-related data collected with Ponsse simulator (TUNI-edition).

## Usage

Record the screen (e.g., with OBS Studio) while running a session in the Ponsse Simulator. After finishing, move the video and the two log files (`MixerEventLog_XXX.csv` and `VarjoEyeTracking_XXX.csv`) into a folder.

In the visualization app, click the 📂 icon and select that folder.

Before replaying for the first time, you need to determine the gap between the start times of the video and the log files. The best way to find this gap is to locate a driving or tree-grabbing event both on the timeline and in the video. The difference between the timestamps shown on the timeline corresponds to the gap. If you started recording the video before launching the session, the gap will be negative.

After determining the gap, enter it in the box next to `Video delays` at the top of the window. The gap value must be in seconds. This value will be saved and automatically restored the next time you load data from the same folder.

To start the replay, click the ▶ button or press the SPACEBAR. You can set the replay start time by clicking on the timeline (only while the replay is paused).

You can use the three buttons below the timeline to enlarge a particular visualization. The 3D gaze plot can be rotated using the right mouse button, and zoomed in/out using the mouse scroll-wheel.