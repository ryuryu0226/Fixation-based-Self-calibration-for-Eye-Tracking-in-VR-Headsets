from typing import List, Tuple
import numpy as np


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

        # rotation matrix
        return np.dot(y_rot, x_rot)
