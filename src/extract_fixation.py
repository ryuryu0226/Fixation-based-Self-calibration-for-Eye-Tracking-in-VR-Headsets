import numpy as np
import pandas as pd
import itertools


class ExtractFixation:

    def __init__(self):
        # Format data
        self.df = None
        self.frame = None
        self.time = None
        self.ray = None  # Gaze direction of the cyclopean eye in camera coordinate
        self.base_ray = None  # Gaze direction (visual axis) of the cyclopean eye in camera coordinate
        self.WorldToCamMat = None  # WorldToCamera matrix (4x4 matrix)
        self.CamToWorldMat = None  # CameraToWorld matrix (3x3 matrix)
        self.CamToWorldPos = None  # HMD position in world coordinate
        self.EyeToWorldPos = None  # Eye position in world coordinate

        # Calculate threshold
        self.start_frame = 100  # Start frame for calculating frame rate
        self.end_frame = 200  # End frame for calculating frame rate
        self.fs = None  # Frame rate
        self.duration = 0.2  # Duration threshold (sec)
        self.duration_frame = None  # Duration threshold (frame)
        self.dig_per_frame = None  # Velocity threshold (degrees per frame)
        self.rad_per_sec = None  # Velocity threshold (radians per second)

        # fixation_calc_ivt
        self.fix_frame = None  # frame of fixation

        # fixation_calc_ict
        # self.ict_th = 0.0003 # 1.0 deg
        self.ict_th = 0.000033  # 2.0 cm

    def formatting(self, path):
        df = pd.read_csv(path)
        self.df = df

        # Replaced by nan when openness is less than 0.5
        openness_l = np.array(df["opennessl"].values)
        openness_r = np.array(df["opennessr"].values)
        openness = np.logical_or(openness_l < 0.5, openness_r < 0.5)

        # Get gaze directions of the cyclopean eye
        ray_x = np.array(df["ray_x"].values, dtype=np.float64)
        ray_y = np.array(df["ray_y"].values, dtype=np.float64)
        ray_z = np.array(df["ray_z"].values, dtype=np.float64)
        # Replaced by nan when openness is less than 0.5
        ray_x[openness] = np.nan
        ray_y[openness] = np.nan
        ray_z[openness] = np.nan
        self.ray = np.stack([ray_x, ray_y, ray_z], axis=1)

        # Get gaze directions (visual axis) of the cyclopean eye
        base_ray_x = np.array(df["ray_x"].values, dtype=np.float64)
        base_ray_y = np.array(df["ray_y"].values, dtype=np.float64)
        base_ray_z = np.array(df["ray_z"].values, dtype=np.float64)
        # Replaced by nan when openness is less than 0.5
        base_ray_x[openness] = np.nan
        base_ray_y[openness] = np.nan
        base_ray_z[openness] = np.nan
        self.base_ray = np.stack([base_ray_x, base_ray_y, base_ray_z], axis=1)

        # Get WorldToCamera matrix
        mat00 = np.array(df["mat_00"].values)
        mat10 = np.array(df["mat_10"].values)
        mat20 = np.array(df["mat_20"].values)
        mat30 = np.array(df["mat_30"].values)
        mat01 = np.array(df["mat_01"].values)
        mat11 = np.array(df["mat_11"].values)
        mat21 = np.array(df["mat_21"].values)
        mat31 = np.array(df["mat_31"].values)
        mat02 = np.array(df["mat_02"].values)
        mat12 = np.array(df["mat_12"].values)
        mat22 = np.array(df["mat_22"].values)
        mat32 = np.array(df["mat_32"].values)
        mat03 = np.array(df["mat_03"].values)
        mat13 = np.array(df["mat_13"].values)
        mat23 = np.array(df["mat_23"].values)
        mat33 = np.array(df["mat_33"].values)

        self.WorldToCamMat = np.empty([mat00.shape[0], 4, 4])
        for i in range(mat00.shape[0]):
            self.WorldToCamMat[i] = np.array([[mat00[i], mat01[i], mat02[i], mat03[i]],
                                              [mat10[i], mat11[i], mat12[i], mat13[i]],
                                              [-mat20[i], -mat21[i], -mat22[i], -mat23[i]],
                                              [mat30[i], mat31[i], mat32[i], mat33[i]]])

        # Get CameraToWorld matrix
        ctw00 = np.array(df["ctw_00"].values)
        ctw10 = np.array(df["ctw_10"].values)
        ctw20 = np.array(df["ctw_20"].values)
        ctw01 = np.array(df["ctw_01"].values)
        ctw11 = np.array(df["ctw_11"].values)
        ctw21 = np.array(df["ctw_21"].values)
        ctw02 = np.array(df["ctw_02"].values)
        ctw12 = np.array(df["ctw_12"].values)
        ctw22 = np.array(df["ctw_22"].values)

        self.CamToWorldMat = np.empty([ctw00.shape[0], 3, 3])
        for i in range(ctw00.shape[0]):
            self.CamToWorldMat[i] = np.array([[ctw00[i], ctw01[i], -ctw02[i]],
                                              [ctw10[i], ctw11[i], -ctw12[i]],
                                              [ctw20[i], ctw21[i], -ctw22[i]]])

        # Get HMD position in world coordinate
        ctw03 = np.array(df["ctw_03"].values)
        ctw13 = np.array(df["ctw_13"].values)
        ctw23 = np.array(df["ctw_23"].values)
        self.CamToWorldPos = np.stack([ctw03, ctw13, ctw23], 1)

        # Get eye position in HMD coordinate
        eye_x = np.array(df["eye_x"].values)
        eye_y = np.array(df["eye_y"].values)
        eye_z = np.array(df["eye_z"].values)
        eye = np.stack([eye_x, eye_y, eye_z], 1)

        # Get eye position in world coordinate
        eye_rotated = np.empty([0, 3])
        for i in range(eye.shape[0]):  # Rotate eye position
            eye_tmp = np.dot(self.CamToWorldMat[i], eye[i])
            eye_rotated = np.vstack([eye_rotated, [eye_tmp[0], eye_tmp[1], eye_tmp[2]]])
        self.EyeToWorldPos = self.CamToWorldPos + eye_rotated  # Translate eye position

        # Get frame and time
        self.frame = np.array(df["frame"].values, dtype=np.int64)
        self.time = np.array(df["time"].values)
        return df.head()

    # Calculate frame rate and threshold
    def calculate_th(self, dig_per_sec=100):
        # calculate frame rate
        record_start = self.time[self.start_frame]
        record_stop = self.time[self.end_frame - 1]
        ts = (record_stop - record_start) / (self.end_frame - self.start_frame)
        self.fs = 1 / ts

        # calculate velocity threshold
        self.dig_per_frame = dig_per_sec / self.fs
        self.rad_per_sec = np.tan((self.dig_per_frame / 180) * np.pi)
        self.duration_frame = int(np.ceil(self.fs * self.duration))
        return "frame rate: %f, duration frame: %d" % (self.fs, self.duration_frame)

    # Calculate angle between two vectors
    def calculate_angle(self, vector1, vector2):
        i = np.inner(vector1, vector2)
        n = np.linalg.norm(vector1) * np.linalg.norm(vector2)
        c = i / n
        return np.rad2deg(np.arccos(np.clip(c, -1.0, 1.0)))

    # I-VT
    def get_fixation_by_ivt(self, duration=10):
        i = 0
        window = np.empty([0, 2])
        while i < self.ray.shape[0] - 1:
            angle = self.calculate_angle(self.ray[i], self.ray[i + 1])
            if angle < self.dig_per_frame:  # Velocity threshold
                start = i
                while angle < self.dig_per_frame:  # Velocity threshold
                    i += 1
                    if i >= self.ray.shape[0] - 1:  # End of the data
                        break
                    angle = self.calculate_angle(self.ray[i], self.ray[i + 1])
                else:
                    stop = i
                    if stop - start >= duration:  # Duration threshold
                        window = np.vstack([window, [start, stop]])
            else:
                i += 1
                if i >= self.ray.shape[0] - 1:  # End of the data
                    break
        self.fix_frame = np.stack([window[:, 0], window[:, 1]], axis=1)
        return self.fix_frame

    # I-VT in world coordinate
    def get_fixation_by_ivt_world(self, duration=10):
        ray_world = np.empty([0, 3])
        for j in range(self.ray.shape[0]):
            ray_world_tmp = np.dot(self.CamToWorldMat[j], self.ray[j])
            ray_world = np.vstack([ray_world, [ray_world_tmp[0], ray_world_tmp[1], ray_world_tmp[2]]])

        i = 0
        window = np.empty([0, 2])
        while i < ray_world.shape[0] - 1:
            angle = self.calculate_angle(ray_world[i], ray_world[i + 1])
            if angle < self.dig_per_frame:  # Velocity threshold
                start = i
                while angle < self.dig_per_frame:  # Velocity threshold
                    i += 1
                    if i >= ray_world.shape[0] - 1:  # End of the data
                        break
                    angle = self.calculate_angle(ray_world[i], ray_world[i + 1])
                else:
                    stop = i
                    if stop - start >= duration:  # Duration threshold
                        window = np.vstack([window, [start, stop]])
            else:
                i += 1
                if i >= ray_world.shape[0] - 1:  # End of the data
                    break
        self.fix_frame = np.stack([window[:, 0], window[:, 1]], axis=1)
        return self.fix_frame

    # Extract gaze directions during a fixation
    def extract_ray(self, fix_on, fix_off):
        return self.ray[int(fix_on):int(fix_off)]

    # Extract gaze directions (visual axis) during a fixation
    def extract_base_ray(self, fix_on, fix_off):
        return self.base_ray[int(fix_on):int(fix_off)]

    # Extract WorldToCamera matrix during a fixation
    def extract_CameraToWorldMat(self, fix_on, fix_off):
        return self.CamToWorldMat[int(fix_on):int(fix_off)]

    # Extract WorldToCamera matrix during a fixation
    def extract_WorldToCameraMat(self, fix_on, fix_off):
        return self.WorldToCamMat[int(fix_on):int(fix_off)]

    # Extract HMD position in world coordinate during a fixation
    def extract_CameraToWorldPos(self, fix_on, fix_off):
        return self.CamToWorldPos[int(fix_on):int(fix_off)]

    # Extract eye position in world coordinate during a fixation
    def extract_EyeToWorldPos(self, fix_on, fix_off):
        return self.EyeToWorldPos[int(fix_on):int(fix_off)]

    # Calculate the maximum distance of eye position during a fixation
    def calculate_head_move(self, fix_on, fix_off):
        EyeToWorldPos = self.EyeToWorldPos[int(fix_on):int(fix_off)]
        dis_list = []
        for pair in itertools.combinations(EyeToWorldPos, 2):
            dis = np.sqrt((pair[0][0] - pair[1][0]) * (pair[0][0] - pair[1][0]) + (pair[0][1] - pair[1][1]) * (
                    pair[0][1] - pair[1][1]) + (pair[0][2] - pair[1][2]) * (pair[0][2] - pair[1][2]))
            dis_list.append(dis)
        dis_max = max(dis_list)
        return dis_max
