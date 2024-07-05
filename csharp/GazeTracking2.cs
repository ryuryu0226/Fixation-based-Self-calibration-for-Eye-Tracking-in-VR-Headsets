using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;
using ViveSR.anipal.Eye;

public class GazeTracking2 : MonoBehaviour
{
    StreamWriter sw;
    Vector3 GazeOriginLeftLocal, GazeDirectionLeftLocal; // Gaze origin and direction from the left eye in local coordinate system
    Vector3 GazeOriginRightLocal, GazeDirectionRightLocal; // Gaze origin and direction from the right eye in local coordinate system
    public bool exe_flag; // Flag indicating exe file or not
    public bool is_recording; // Flag indicating whether to record or not
    private int hash_prev = -1;
    private static EyeData eyeData = new EyeData();
    int frame = 0; // Frame number
    int gaze_flag = 0; // Flag indicating the marker number
    public GameObject LeftGazePoint, RightGazePoint, CenterGazePoint; // Sphere object to display the gaze point

    private static void EyeCallback(ref EyeData eye_data)
    {
        eyeData = eye_data;
    }

    // convert gaze poistion on calibration plane at 1m away (HMD coordinate)
    Vector2 gaze_hmd_1m(Vector3 ray, Vector3 eye)
    {
        float t = (1 - eye.z) / ray.z;
        float xh = eye.x + t * ray.x;
        float yh = eye.y + t * ray.y;

        Vector2 gaze = new Vector2(xh, yh);
        return gaze;
    }

    void Start()
    {
        string date = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        if (exe_flag)
        {
            sw = new StreamWriter(Application.dataPath + @"\" + date + ".csv", false);
        }
        else
        {
            sw = new StreamWriter(@"data\" + date + ".csv", false); // true = append, false = overwrite
        }

        string[] header_array = {"flag", "xl", "yl", "zl", "xr", "yr", "zr", "xc", "yc", "zc", // gaze position in HMD coordinate
                        "mat_00", "mat_10", "mat_20", "mat_30", "mat_01", "mat_11", "mat_21", "mat_31", // WorldToCamera Matrix
                        "mat_02", "mat_12", "mat_22", "mat_32", "mat_03", "mat_13", "mat_23", "mat_33",
                        "ctw_00", "ctw_10", "ctw_20", "ctw_30", "ctw_01", "ctw_11", "ctw_21", "ctw_31", // CameraToWorld Matrix
                        "ctw_02", "ctw_12", "ctw_22", "ctw_32", "ctw_03", "ctw_13", "ctw_23", "ctw_33",
                        "opennessl", "opennessr", "ray_x", "ray_y", "ray_z", "eye_x", "eye_y", "eye_z", // gaze direction and eye position in HMD coordinate
                        "time", "frame"};
        string header = string.Join(",", header_array);
        sw.WriteLine(header);
    }

     void FixedUpdate()
    {
        SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
        if (is_recording)
        {
            int hash = eyeData.verbose_data.GetHashCode();
            if (hash == hash_prev) return;
            hash_prev = hash;
        }

        // Get the gaze ray from the left eye
        SRanipal_Eye.GetGazeRay(GazeIndex.LEFT, out GazeOriginLeftLocal, out GazeDirectionLeftLocal);
        // Transform the local gaze origin to world coordinates
        Vector3 GazeOriginLeft = Camera.main.transform.TransformPoint(GazeOriginLeftLocal);
        // Transform the local gaze direction to world coordinates
        Vector3 GazeDirectionLeft = Camera.main.transform.TransformDirection(GazeDirectionLeftLocal);

        // Get the gaze ray from the right eye
        SRanipal_Eye.GetGazeRay(GazeIndex.RIGHT, out GazeOriginRightLocal, out GazeDirectionRightLocal);
        // Transform the local gaze origin to world coordinates
        Vector3 GazeOriginRight = Camera.main.transform.TransformPoint(GazeOriginRightLocal);
        // Transform the local gaze direction to world coordinates
        Vector3 GazeDirectionRight = Camera.main.transform.TransformDirection(GazeDirectionRightLocal);

        // Average gaze origin
        Vector3 GazeOriginLocal = (GazeOriginLeftLocal + GazeOriginRightLocal) / 2;
        Vector3 GazeOrigin = Camera.main.transform.TransformPoint(GazeOriginLocal);
        // Average gaze direction
        Vector2 gaze_l = gaze_hmd_1m(GazeDirectionLeftLocal, GazeOriginLeftLocal);
        Vector2 gaze_r = gaze_hmd_1m(GazeDirectionRightLocal, GazeOriginRightLocal);
        Vector2 gaze = (gaze_l + gaze_r) / 2;
        Vector3 GazeDirectionLocal = new Vector3(gaze.x-GazeOriginLocal.x, gaze.y-GazeOriginLocal.y, 1.0f-GazeOriginLocal.z).normalized;
        Vector3 GazeDirection = Camera.main.transform.TransformDirection(GazeDirectionLocal);

        RaycastHit hitL, hitR, hitC;
        string str1="", str2 = "", str3="", str4="", str5="";
        if (Physics.Raycast(GazeOriginLeft, GazeDirectionLeft, out hitL, 50f)) // Raycast from the left eye
        {
            Vector3 PointL = Camera.main.transform.InverseTransformPoint(hitL.point); // convert world coordinate to camera coordinate
            str1 = PointL.x + "," + PointL.y + "," + PointL.z;
            LeftGazePoint.transform.position = hitL.point; // Display the gaze point
        }
        else
        {
            str1 = null + "," + null + "," + null;
            LeftGazePoint.transform.position = hitL.point; // Display the gaze point
        }

        if (Physics.Raycast(GazeOriginRight, GazeDirectionRight, out hitR, 50f)) // Raycast from the right eye
        {
            Vector3 PointR = Camera.main.transform.InverseTransformPoint(hitR.point); // convert world coordinate to camera coordinate
            str2 = PointR.x + "," + PointR.y + "," + PointR.z;
            RightGazePoint.transform.position = hitR.point; // Display the gaze point
        }
        else
        {
            str2 = null + "," + null + "," + null;
            RightGazePoint.transform.position = hitR.point; // Display the gaze point
        }

        if (Physics.Raycast(GazeOrigin, GazeDirection, out hitC, 50f)) // Raycast from the cyclopean eye
        {
            Vector3 PointC = Camera.main.transform.InverseTransformPoint(hitC.point); // convert world coordinate to camera coordinate
            str3 = PointC.x + "," + PointC.y + "," + PointC.z;
            CenterGazePoint.transform.position = hitC.point; // Display the gaze point
        }
        else
        {
            str3 = null + "," + null + "," + null;
            CenterGazePoint.transform.position = hitC.point; // Display the gaze point
        }

        // Get the rotation matrix
        str4 = Camera.main.worldToCameraMatrix[0, 0] + "," + Camera.main.worldToCameraMatrix[1, 0] + "," + Camera.main.worldToCameraMatrix[2, 0] + "," + Camera.main.worldToCameraMatrix[3, 0] + ","
                + Camera.main.worldToCameraMatrix[0, 1] + "," + Camera.main.worldToCameraMatrix[1, 1] + "," + Camera.main.worldToCameraMatrix[2, 1] + "," + Camera.main.worldToCameraMatrix[3, 1] + ","
                + Camera.main.worldToCameraMatrix[0, 2] + "," + Camera.main.worldToCameraMatrix[1, 2] + "," + Camera.main.worldToCameraMatrix[2, 2] + "," + Camera.main.worldToCameraMatrix[3, 2] + ","
                + Camera.main.worldToCameraMatrix[0, 3] + "," + Camera.main.worldToCameraMatrix[1, 3] + "," + Camera.main.worldToCameraMatrix[2, 3] + "," + Camera.main.worldToCameraMatrix[3, 3] + ","
                + Camera.main.cameraToWorldMatrix[0, 0] + "," + Camera.main.cameraToWorldMatrix[1, 0] + "," + Camera.main.cameraToWorldMatrix[2, 0] + "," + Camera.main.cameraToWorldMatrix[3, 0] + ","
                + Camera.main.cameraToWorldMatrix[0, 1] + "," + Camera.main.cameraToWorldMatrix[1, 1] + "," + Camera.main.cameraToWorldMatrix[2, 1] + "," + Camera.main.cameraToWorldMatrix[3, 1] + ","
                + Camera.main.cameraToWorldMatrix[0, 2] + "," + Camera.main.cameraToWorldMatrix[1, 2] + "," + Camera.main.cameraToWorldMatrix[2, 2] + "," + Camera.main.cameraToWorldMatrix[3, 2] + ","
                + Camera.main.cameraToWorldMatrix[0, 3] + "," + Camera.main.cameraToWorldMatrix[1, 3] + "," + Camera.main.cameraToWorldMatrix[2, 3] + "," + Camera.main.cameraToWorldMatrix[3, 3];

        // Get the eye openness
        float opennessL = 0.0f;
        float opennessR = 0.0f;
        SRanipal_Eye.GetEyeOpenness(EyeIndex.LEFT, out opennessL, eyeData);
        SRanipal_Eye.GetEyeOpenness(EyeIndex.RIGHT, out opennessR, eyeData);
        // increment frame number
        frame++;
        str5 = opennessL + "," + opennessR + "," + GazeDirectionLocal.x + "," + GazeDirectionLocal.y + "," + GazeDirectionLocal.z + ","
                + GazeOriginLocal.x + "," + GazeOriginLocal.y + "," +GazeOriginLocal.z + ","
                + Time.time + "," + frame;

        // Write the gaze data to the file
        string str6 = gaze_flag + "," + str1 + "," + str2 + "," + str3 + "," + str4 + "," + str5;
        sw.WriteLine(str6);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}