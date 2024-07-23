# Fixation-based Self-calibration for Eye Tracking in VR Headsets
This repository contains the data, task code and analysis code for the paper "Fixation-based Self-calibration for Eye Tracking in VR Headsets".
However, this repository does not include the 3D models used in our experiment, as the models are prohibited for redistribution.
The 3D models used in our experiment are available
on Unity AssetStore ([Office Interior Archviz](https://assetstore.unity.com/packages/3d/environments/urban/office-interior-archviz-155701)
and [Modern Supermarket](https://assetstore.unity.com/packages/3d/environments/modern-supermarket-186122)).

## Setup
1. Clone the repository
2. Create a virtual environment using `python -m venv .venv`
3. Install the required packages using `pip install -r requirements.txt`

## Directory Structure
```text
./csharp                                # C# sample code for the Unity application
./notebooks                             # Jupyter notebooks for the experiments
├── data                                # Experiment data
│   ├── eval_data                        # Evaluation data obtained by gazing at the calibration markers
│   ├── office_data                     # Raw data acquired while walking through the office environment
│   ├── office_data_opt                 # Office data calibrated to optical axis
│   ├── office_data_vis                 # Office data calibrated to visual axis
│   ├── supermarket_data                # Raw data acquired while walking through the supermarket environment
│   ├── supermarket_data_opt            # Supermarket data calibrated to optical axis
│   └── supermarket_data_vis            # Supermarket data calibrated to visual axis
├── figures                             # Experimental results
├── param                               # Parameter data
│   ├── exp_distance                    # Data for evaluating convergence performance
│   ├── exp_init_param                  # Data for evaluating dependence on initial parameters
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
