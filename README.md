# Fixation-based Self-calibration for Eye Tracking in VR Headsets
This repository contains the code and data for the paper "Fixation-based Self-calibration for Eye Tracking in VR Headsets".

## Setup
1. Clone the repository
2. Create a virtual environment using `python -m venv .venv`
3. Install the required packages using `pip install -r requirements.txt`

## Directory Structure
```text
./csharp                        # C# sample code for the Unity application
./notebooks                     # Jupyter notebooks for the experiments
   ├──data                      # Data used in the experiments
   ├──param                     # Parameters for the optimization
   ├──precalibration            # Pre-calibration for the experiments
   └──user_analysis             # Visualization of the participants' gaze data
./src                           # Modules
```