from typing import List, Tuple
import numpy as np
import pandas as pd


class EvaluateGaze:
    def __init__(self):
        pass

    @staticmethod
    def calibrate_reg(param: List[float],
                      x: np.ndarray, y: np.ndarray) -> Tuple[np.ndarray, np.ndarray]:
        """
        calibration function using regression model.

        :param param: List of float parameters for the regression model.
        :param x: Numpy array representing the x-coordinate of the gaze direction.
        :param y: Numpy array representing the y-coordinate of the gaze direction.
        :return: Tuple containing calibrated x and y coordinates.
        """
        ones = np.ones_like(x)
        zeros = np.zeros_like(x)
        x_lis = np.array([ones, x, y, zeros, zeros, zeros])
        y_lis = np.array([zeros, zeros, zeros, ones, x, y])
        x_calib = np.dot(param, x_lis)
        y_calib = np.dot(param, y_lis)
        return x_calib, y_calib

    @staticmethod
    def calibrate_3d(param: List[float], ray: np.ndarray) -> np.ndarray:
        """
        calibration function using 3D eye model.

        :param param: List of float parameters for the regression model.
        :param ray: Numpy array representing the gaze direction.
        :return: Numpy array containing the calibrated gaze direction.
        """
        # rotation matrix
        rot = np.array([[param[0], param[1], param[2]],
                        [param[3], param[4], param[5]],
                        [param[6], param[7], param[8]]])

        # calibration
        calib_ray_list = np.empty([0, 3])
        for i in range(ray.shape[0]):
            calib_ray = np.dot(rot, ray[i])
            calib_ray_list = np.vstack([calib_ray_list, calib_ray])
        return calib_ray_list

    @staticmethod
    def err_rmse_reg(param: List[float], xe: np.ndarray, ye: np.ndarray,
                     eye_x: np.ndarray, eye_y: np.ndarray, eye_z: np.ndarray,
                     p: np.ndarray, q: np.ndarray) -> float:
        """
        Calculate the square error using regression model.

        :param param: List of float parameters for the regression model.
        :param xe: Numpy array representing the x-coordinate of the gaze direction.
        :param ye: Numpy array representing the y-coordinate of the gaze direction.
        :param eye_x: Numpy array representing the x-coordinate of the eye position.
        :param eye_y: Numpy array representing the y-coordinate of the eye position.
        :param eye_z: Numpy array representing the z-coordinate of the eye position.
        :param p: Numpy array representing the x-coordinate of the marker position.
        :param q: Numpy array representing the y-coordinate of the marker position.
        :return: float value representing the root-mean-square error.
        """
        # calibrate gaze direction
        calib_xe, calib_ye = EvaluateGaze.calibrate_reg(param, xe, ye)

        # convert gaze position on calibration plane at 1m away (HMD coordinates)
        ones = np.ones_like(calib_xe)
        t = (ones - eye_z) / 1.0
        xh = eye_x + t * calib_xe
        yh = eye_y + t * calib_ye

        # calculate error
        err_x = xh - p
        err_y = yh - q
        return np.mean(np.sqrt(err_x * err_x + err_y * err_y))

    @staticmethod
    def err_rmse_3d(param: List[float], ray: np.ndarray,
                    eye_x: np.ndarray, eye_y: np.ndarray, eye_z: np.ndarray,
                    p: np.ndarray, q: np.ndarray) -> float:
        """
        Calculate the square error using 3D eye model.

        :param param: List of float parameters for the regression model.
        :param ray: Numpy array representing the gaze direction.
        :param eye_x: Numpy array representing the x-coordinate of the eye position.
        :param eye_y: Numpy array representing the y-coordinate of the eye position.
        :param eye_z: Numpy array representing the z-coordinate of the eye position.
        :param p: Numpy array representing the x-coordinate of the marker position.
        :param q: Numpy array representing the y-coordinate of the marker position.
        :return: float value representing the root-mean-square error.
        """
        # calibrate gaze direction
        calib_ray = EvaluateGaze.calibrate_3d(param, ray)

        # calculate gaze direction at z=1
        calib_xe = calib_ray[:, 0] / calib_ray[:, 2] * 1.0
        calib_ye = calib_ray[:, 1] / calib_ray[:, 2] * 1.0

        # convert gaze position on calibration plane at 1m away (HMD coordinates)
        ones = np.ones_like(calib_xe)
        t = (ones - eye_z) / 1.0
        xh = eye_x + t * calib_xe
        yh = eye_y + t * calib_ye

        # calculate error
        err_x = xh - p
        err_y = yh - q
        return np.mean(np.sqrt(err_x * err_x + err_y * err_y))

    @staticmethod
    def get_rotation(param: List[float]) -> np.ndarray:
        """
        Get the rotation matrix.

        :param param: List of float parameters for the rotation matrix.
        :return: Numpy array representing the rotation matrix.
        """
        # convert degree to radian
        alpha = np.deg2rad(param[0])
        beta = np.deg2rad(param[1])

        # horizontal rotation
        y_rot = np.array([[np.cos(alpha), 0.0, np.sin(alpha)],
                          [0.0, 1.0, 0.0],
                          [-np.sin(alpha), 0.0, np.cos(alpha)]])

        # vertical rotation
        x_rot = np.array([[1.0, 0.0, 0.0],
                          [0.0, np.cos(beta), np.sin(beta)],
                          [0.0, -np.sin(beta), np.cos(beta)]])

        # return rotation matrix
        return np.dot(y_rot, x_rot)

    @staticmethod
    def get_rotation_inv(param: List[float]) -> np.ndarray:
        """
        Get the inverse rotation matrix.

        :param param: List of float parameters for the rotation matrix.
        :return: Numpy array representing the inverse rotation matrix.
        """
        # convert degree to radian
        alpha = np.deg2rad(param[0])
        beta = np.deg2rad(param[1])

        # horizontal rotation
        y_rot = np.array([[np.cos(alpha), 0.0, np.sin(alpha)],
                          [0.0, 1.0, 0.0],
                          [-np.sin(alpha), 0.0, np.cos(alpha)]])
        y_rot_inv = np.linalg.inv(y_rot)

        # vertical rotation
        x_rot = np.array([[1.0, 0.0, 0.0],
                          [0.0, np.cos(beta), np.sin(beta)],
                          [0.0, -np.sin(beta), np.cos(beta)]])
        x_rot_inv = np.linalg.inv(x_rot)

        # return inverse rotation matrix
        return np.dot(y_rot_inv, x_rot_inv)

    @staticmethod
    def get_absolute_error(gaze_data_path: str,
                           param_base: np.ndarray,
                           param: np.ndarray) -> float:
        """
        Calculate the absolute error.

        :param gaze_data_path: File path of the gaze data.
        :param param_base: Numpy array representing the visual axis parameters.
        :param param: Numpy array representing the calibration parameters.
        :return: float value representing the absolute error.
        """
        # load data
        df = pd.read_csv(gaze_data_path)
        # gaze direction
        ray_x = df["ray_x"].values
        ray_y = df["ray_y"].values
        ray_z = df["ray_z"].values
        # gaze direction at z=1
        xe = ray_x[:] / ray_z[:] * 1.0
        ye = ray_y[:] / ray_z[:] * 1.0
        # marker position on a calibration plane at 1m away (HMD coordinates)
        p = df["u"].values
        q = df["v"].values
        # origin (HMD coordinates)
        eye_x = df["eye_x"].values
        eye_y = df["eye_y"].values
        eye_z = df["eye_z"].values

        # calibrate gaze direction
        calib_xe, calib_ye = EvaluateGaze.calibrate_reg(param_base, xe, ye)
        r = np.sqrt(calib_xe * calib_xe + calib_ye * calib_ye + 1)
        base_ray = np.stack([calib_xe / r, calib_ye / r, 1 / r], axis=1)

        # 精度評価
        rmse_error = EvaluateGaze.err_rmse_3d(param, base_ray, eye_x, eye_y, eye_z, p, q)
        absolute_error = np.rad2deg(rmse_error)

        return absolute_error
