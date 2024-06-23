from typing import List
import numpy as np
import pandas as pd
from evaluate_gaze import EvaluateGaze


class PrecalibrateGaze:

    def __init__(self, path: str):
        self.param_base = pd.read_csv(path).values
        print(f"param_base.shape: {self.param_base.shape}")

    def precalibrate_gaze(self, input_path: str, output_path: str,
                          user_id: int, param: List[float]) -> None:
        """
        Pre-calibrate gaze data using regression model and 3D eye model.

        :param input_path: String path to the input file.
        :param output_path: String path to the output file.
        :param user_id: Integer user ID.
        :param param: List of float parameters for the rotation matrix.
        :return: None
        """
        df = pd.read_csv(input_path)
        df = df.iloc[:, 10:]
        print(f"df.shape: {df.shape}")

        # gaze direction
        ray_x = df["ray_x"].values
        ray_y = df["ray_y"].values
        ray_z = df["ray_z"].values

        # gaze direction at z=1
        xe = ray_x[:] / ray_z[:] * 1.0
        ye = ray_y[:] / ray_z[:] * 1.0

        # calibrate gaze direction using regression model
        calib_xe, calib_ye = EvaluateGaze.calibrate_reg(self.param_base[user_id - 1], xe, ye)

        # vector magnitude to normalize
        ones = np.ones_like(calib_xe)
        base_dis = np.sqrt(calib_xe * calib_xe + calib_ye * calib_ye + ones * ones)
        base_ray = np.stack([calib_xe / base_dis, calib_ye / base_dis, ones / base_dis], axis=1)

        # calibrate gaze direction using 3D eye model
        param_3d = EvaluateGaze.get_rotation_inv(param).flatten()
        calib_ray = EvaluateGaze.calibrate_3d(param_3d, base_ray)

        # update dataframe
        df = df.drop(["ray_x", "ray_y", "ray_z"], axis=1)
        df.insert(len(df.columns), "ray_x", calib_ray[:, 0], True)
        df.insert(len(df.columns), "ray_y", calib_ray[:, 1], True)
        df.insert(len(df.columns), "ray_z", calib_ray[:, 2], True)
        df.insert(len(df.columns), "base_x", base_ray[:, 0], True)
        df.insert(len(df.columns), "base_y", base_ray[:, 1], True)
        df.insert(len(df.columns), "base_z", base_ray[:, 2], True)

        df.to_csv(output_path, index=False, encoding="utf_8_sig")
        print(f"updated df.shape: {df.shape}")
