# Fixation-based Self-calibration for Eye Tracking in VR Headsets
This repository contains the code and data for the paper "Fixation-based Self-calibration for Eye Tracking in VR Headsets".

## Setup
1. Clone the repository
2. Create a virtual environment using `python -m venv .venv`
3. Install the required packages using `pip install -r requirements.txt`

## Directory Structure
```text
./csharp                                # C# sample code for the Unity application
./notebooks                             # Jupyter notebooks for the experiments
├── data                                # Experiment data
│   ├── evaldata                        # Evaluation data obtained by gazing at the calibration markers
│   ├── office_data                     # Raw data acquired while walking through the office environment
│   ├── office_data_opt                 # Office data calibrated to optical axis
│   ├── office_data_vis                 # Office data calibrated to visual axis
│   ├── supermarket_data                # Raw data acquired while walking through the supermarket environment
│   ├── supermarket_data_opt            # Supermarket data calibrated to optical axis
│   └── supermarket_data_vis            # Supermarket data calibrated to visual axis
├── figures                             # Experimental results
├── param                               # Parameter data
│   ├── idt_07deg_160ms_opt             # Parameters obtained by I-DT (opt)
│   ├── idt_07deg_160ms_vis             # Parameters obtained by I-DT (vis)
│   ├── ivdt_80deg_07deg_160ms_opt      # Parameters obtained by I-VDT (opt)
│   ├── ivdt_80deg_07deg_160ms_opt      # Parameters obtained by I-VDT (vis)
│   ├── ivtl_80deg_160ms_opt            # Parameters obtained by I-VT (opt)
│   └── ivtl_80deg_160ms_opt            # Parameters obtained by I-VT (vis)
├── precalibration                      # Pre-calibration for the experiments
├── result_graph                        # Evaluation of our self-calibration method
└── user_analysis                       # Visualization of the participants' gaze data
./src                                   # Modules
```