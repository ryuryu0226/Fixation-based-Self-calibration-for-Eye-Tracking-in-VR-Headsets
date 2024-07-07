using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Python.Runtime;

public class OptimizeUtil : MonoBehaviour
{
    public static string python_home; // your python home directory
    public static string python_src; // your python module directory

    public static void AddEnvPath(params string[] paths)
    {
        var envPaths = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator).ToList();
        foreach (var path in paths)
        {
            if (path.Length > 0 && !envPaths.Contains(path))
            {
                envPaths.Insert(0, path);
            }
        }
        Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator.ToString(), envPaths), EnvironmentVariableTarget.Process);
    }

    // Configure Python environment
    public static void PySetting()
    {
        var PYTHON_HOME = Environment.ExpandEnvironmentVariables(python_home);

        AddEnvPath(
            PYTHON_HOME,
            Path.Combine(PYTHON_HOME, @"Dlls")
        );

        PythonEngine.PythonHome = PYTHON_HOME; // configure python home directory
        PythonEngine.PythonPath = string.Join( // cofigure python module directory
            Path.PathSeparator.ToString(),
            new string[] {
                PythonEngine.PythonPath, // original python path
                Path.Combine(PYTHON_HOME, @"Lib\site-packages"), // site-packages
                Path.Combine(python_src), // your python module directory
            }
        );
    }

    public static void ExtractFixationByIVT(string file, out List<List<Vector3>> BaseRay, out List<List<List<float>>> CamToWorldMat, out List<List<List<float>>> WorldToCamMat, out List<List<Vector3>> EyeToWorldPos, out List<float> EyePosMove, int duration=10, float deg_per_sec=100.0f, bool local=true)
    {
        PythonEngine.Initialize(mode: ShutdownMode.Reload);

        using (Py.GIL()) // Get Global Interpreter Lock
        {
            PythonEngine.RunSimpleString($"import sys;sys.path.insert(1, '{python_src}');");
            dynamic mylib = Py.Import("extraction_fixation");
            dynamic Fix = mylib.ExtractFixation();

            Debug.Log("Data Loading");
            dynamic table = Fix.formatting(file);

            dynamic frame_rate = Fix.calculate_th(deg_per_sec);
            Debug.Log(frame_rate);
            dynamic window;
            if (local) // I-VT in camera coordinate
            {
                // Get the index of the gaze data identified as fixation
                window = Fix.get_fixation_by_ivt(duration);
            }
            else // I-VT in world coordinate
            {
                // Get the index of the gaze data identified as fixation
                window = Fix.get_fixation_by_ivt_world(duration);
            }

            BaseRay = new  List<List<Vector3>>(); // Gaze direction at the visaual axis parameter
            CamToWorldMat = new List<List<List<float>>>(); // Camera (hmd or eye) to world rotation matrix
            WorldToCamMat = new List<List<List<float>>>(); // World to camera (hmd or eye) rotation matrix
            EyeToWorldPos = new List<List<Vector3>>(); // Eye position in world coordinate system
            EyePosMove = new List<float>(); // List of eye movement

            for (int i=0; i<(int)window.shape[0]; i++)
            {
                dynamic fix_base_ray = Fix.extract_base_ray(window[i][0], window[i][1]);
                dynamic fix_CamToWorldMat = Fix.extract_CameraToWorldMat(window[i][0], window[i][1]);
                dynamic fix_WorldToCamMat = Fix.extract_WorldToCameraMat(window[i][0], window[i][1]);
                dynamic fix_EyeToWorldPos = Fix.extract_EyeToWorldPos(window[i][0], window[i][1]);
                float fix_EyePosMove = (float)Fix.calculate_head_move(window[i][0], window[i][1]);
                EyePosMove.Add(fix_EyePosMove);

                List<Vector3> BaseRay_list = new  List<Vector3>();
                List<List<float>> CamToWorldMat_list = new List<List<float>>();
                List<List<float>> WorldToCamMat_list = new List<List<float>>();
                List<Vector3> EyeToWorldPos_list = new List<Vector3>();

                for (int j=0; j<(int)fix_base_ray.shape[0]; j++)
                {
                    BaseRay_list.Add(new Vector3((float)fix_base_ray[j][0], (float)fix_base_ray[j][1], (float)fix_base_ray[j][2]));

                    CamToWorldMat_list.Add(new List<float> {(float)fix_CamToWorldMat[j][0][0], (float)fix_CamToWorldMat[j][0][1], (float)fix_CamToWorldMat[j][0][2],
                                                            (float)fix_CamToWorldMat[j][1][0], (float)fix_CamToWorldMat[j][1][1], (float)fix_CamToWorldMat[j][1][2],
                                                            (float)fix_CamToWorldMat[j][2][0], (float)fix_CamToWorldMat[j][2][1], (float)fix_CamToWorldMat[j][2][2]});

                    WorldToCamMat_list.Add(new List<float> {(float)fix_WorldToCamMat[j][0][0], (float)fix_WorldToCamMat[j][0][1], (float)fix_WorldToCamMat[j][0][2],
                                                            (float)fix_WorldToCamMat[j][1][0], (float)fix_WorldToCamMat[j][1][1], (float)fix_WorldToCamMat[j][1][2],
                                                            (float)fix_WorldToCamMat[j][2][0], (float)fix_WorldToCamMat[j][2][1], (float)fix_WorldToCamMat[j][2][2]});

                    EyeToWorldPos_list.Add(new Vector3((float)fix_EyeToWorldPos[j][0], (float)fix_EyeToWorldPos[j][1], (float)fix_EyeToWorldPos[j][2]));
                }

                BaseRay.Add(BaseRay_list);
                CamToWorldMat.Add(CamToWorldMat_list);
                WorldToCamMat.Add(WorldToCamMat_list);
                EyeToWorldPos.Add(EyeToWorldPos_list);
            }
        }
        PythonEngine.Shutdown();
    }

    public static void ExtractFixation(string file, List<List<int>> window, out List<List<Vector3>> BaseRay, out List<List<List<float>>> CamToWorldMat, out List<List<List<float>>> WorldToCamMat, out List<List<Vector3>> EyeToWorldPos, out List<float> EyePosMove)
    {
        PythonEngine.Initialize(mode: ShutdownMode.Reload);

        using (Py.GIL()) // Get Global Interpreter Lock
        {
            PythonEngine.RunSimpleString($"import sys;sys.path.insert(1, '{python_src}');");
            dynamic mylib = Py.Import("extraction_fixation");
            dynamic Fix = mylib.ExtractFixation();

            Debug.Log("Data Loading");
            dynamic table = Fix.formatting(file);

            dynamic frame_rate = Fix.calculate_th();
            Debug.Log(frame_rate);

            BaseRay = new  List<List<Vector3>>(); // Gaze direction at the visaual axis parameter
            CamToWorldMat = new List<List<List<float>>>(); // Camera (hmd or eye) to world rotation matrix
            WorldToCamMat = new List<List<List<float>>>(); // World to camera (hmd or eye) rotation matrix
            EyeToWorldPos = new List<List<Vector3>>(); // Eye position in world coordinate system
            EyePosMove = new List<float>(); // List of eye movement

            for (int i=0; i< window.Count; i++)
            {
                dynamic fix_base_ray = Fix.extract_base_ray(window[i][0], window[i][1]);
                dynamic fix_CamToWorldMat = Fix.extract_CameraToWorldMat(window[i][0], window[i][1]);
                dynamic fix_WorldToCamMat = Fix.extract_WorldToCameraMat(window[i][0], window[i][1]);
                dynamic fix_EyeToWorldPos = Fix.extract_EyeToWorldPos(window[i][0], window[i][1]);
                float fix_EyePosMove = (float)Fix.calculate_head_move(window[i][0], window[i][1]);
                EyePosMove.Add(fix_EyePosMove);

                List<Vector3> BaseRay_list = new  List<Vector3>();
                List<List<float>> CamToWorldMat_list = new List<List<float>>();
                List<List<float>> WorldToCamMat_list = new List<List<float>>();
                List<Vector3> EyeToWorldPos_list = new List<Vector3>();

                for (int j=0; j<(int)fix_base_ray.shape[0]; j++)
                {
                    BaseRay_list.Add(new Vector3((float)fix_base_ray[j][0], (float)fix_base_ray[j][1], (float)fix_base_ray[j][2]));

                    CamToWorldMat_list.Add(new List<float> {(float)fix_CamToWorldMat[j][0][0], (float)fix_CamToWorldMat[j][0][1], (float)fix_CamToWorldMat[j][0][2],
                                                            (float)fix_CamToWorldMat[j][1][0], (float)fix_CamToWorldMat[j][1][1], (float)fix_CamToWorldMat[j][1][2],
                                                            (float)fix_CamToWorldMat[j][2][0], (float)fix_CamToWorldMat[j][2][1], (float)fix_CamToWorldMat[j][2][2]});

                    WorldToCamMat_list.Add(new List<float> {(float)fix_WorldToCamMat[j][0][0], (float)fix_WorldToCamMat[j][0][1], (float)fix_WorldToCamMat[j][0][2],
                                                            (float)fix_WorldToCamMat[j][1][0], (float)fix_WorldToCamMat[j][1][1], (float)fix_WorldToCamMat[j][1][2],
                                                            (float)fix_WorldToCamMat[j][2][0], (float)fix_WorldToCamMat[j][2][1], (float)fix_WorldToCamMat[j][2][2]});

                    EyeToWorldPos_list.Add(new Vector3((float)fix_EyeToWorldPos[j][0], (float)fix_EyeToWorldPos[j][1], (float)fix_EyeToWorldPos[j][2]));
                }

                BaseRay.Add(BaseRay_list);
                CamToWorldMat.Add(CamToWorldMat_list);
                WorldToCamMat.Add(WorldToCamMat_list);
                EyeToWorldPos.Add(EyeToWorldPos_list);
            }
        }
        PythonEngine.Shutdown();
    }

    public static void GetFixationByIDT(string file, float dispersion_th, out List<List<int>> window, int duration_th=10)
    {
        PythonEngine.Initialize(mode: ShutdownMode.Reload);

       using (Py.GIL()) // Get Global Interpreter Lock
        {
            PythonEngine.RunSimpleString($"import sys;sys.path.insert(1, '{python_src}');");
            dynamic mylib = Py.Import("extraction_fixation");
            dynamic Fix = mylib.ExtractFixation();

            Debug.Log("Data Loading");
            dynamic table = Fix.formatting(file);

            dynamic frame_rate = Fix.calculate_th();
            Debug.Log(frame_rate);

            dynamic Ray_py = Fix.ray; // Gaze direction
            dynamic CamToWorldMat_py = Fix.CamToWorldMat; // Camera (hmd or eye) to world rotation matrix (3x3 matrix)
            dynamic WorldToCamMat_py = Fix.WorldToCamMat; // World to camera (hmd or eye) rotation matrix (4x4 matrix)
            dynamic EyeToWorldPos_py = Fix.EyeToWorldPos; // Eye position in world coordinate system

            List<Vector3> Ray_all = new List<Vector3>();
            List<List<float>> CamToWorldMat_all = new List<List<float>>(); // 3x3 matrix
            List<List<float>> WorldToCamMat_all = new List<List<float>>(); // 3x3 matrix
            List<Vector3> EyeToWorldPos_all = new List<Vector3>();
            for (int i=0; i<(int)Ray_py.shape[0]; i++)
            {
                Ray_all.Add(new Vector3((float)Ray_py[i][0], (float)Ray_py[i][1], (float)Ray_py[i][2]));
                CamToWorldMat_all.Add(new List<float> {(float)CamToWorldMat_py[i][0][0], (float)CamToWorldMat_py[i][0][1], (float)CamToWorldMat_py[i][0][2],
                                                        (float)CamToWorldMat_py[i][1][0], (float)CamToWorldMat_py[i][1][1], (float)CamToWorldMat_py[i][1][2],
                                                        (float)CamToWorldMat_py[i][2][0], (float)CamToWorldMat_py[i][2][1], (float)CamToWorldMat_py[i][2][2]});
                WorldToCamMat_all.Add(new List<float> {(float)WorldToCamMat_py[i][0][0], (float)WorldToCamMat_py[i][0][1], (float)WorldToCamMat_py[i][0][2],
                                                        (float)WorldToCamMat_py[i][1][0], (float)WorldToCamMat_py[i][1][1], (float)WorldToCamMat_py[i][1][2],
                                                        (float)WorldToCamMat_py[i][2][0], (float)WorldToCamMat_py[i][2][1], (float)WorldToCamMat_py[i][2][2]});
                EyeToWorldPos_all.Add(new Vector3((float)EyeToWorldPos_py[i][0], (float)EyeToWorldPos_py[i][1], (float)EyeToWorldPos_py[i][2]));
            }

            // I-DT
            int id = 0;
            window = new List<List<int>>();
            List<Vector3> fix_pos_list = new List<Vector3>(); // Points of regard
            List<Vector3> Ray_list = new List<Vector3>();
            List<List<float>> CamToWorldMat_list = new List<List<float>>(); // 3x3 matrix
            List<List<float>> WorldToCamMat_list = new List<List<float>>(); // 3x3 matrix
            List<Vector3> EyeToWorldPos_list = new List<Vector3>();
            while (id + duration_th < (int)Ray_py.shape[0])
            {
                int start = id; // Index of the fixation start
                int stop = id; // Index of the fixation end

                // Check the PoRs in the initial window
                while (id - start < duration_th)
                {
                    // End of the data
                    if (id >= (int)Ray_py.shape[0]) break;

                    // In case that the gaze direction is NaN
                    if (float.IsNaN(Ray_all[id].x) | float.IsNaN(Ray_all[id].y) | float.IsNaN(Ray_all[id].z))
                    {
                        // Initialize the list
                        fix_pos_list = new List<Vector3>(); // Points of regard
                        Ray_list = new List<Vector3>();
                        CamToWorldMat_list = new List<List<float>>(); // 3x3 matrix
                        WorldToCamMat_list = new List<List<float>>(); // 3x3 matrix
                        EyeToWorldPos_list = new List<Vector3>();

                        id++; // Update current index
                        start = id; // Index of the fixation start
                    }
                    else
                    {
                        // Calculate the point of regard
                        Vector3 fix_pos = CalculatePoR(Ray_all[id], CamToWorldMat_all[id], EyeToWorldPos_all[id]);
                        fix_pos_list.Add(fix_pos);
                        Ray_list.Add(Ray_all[id]);
                        CamToWorldMat_list.Add(CamToWorldMat_all[id]);
                        WorldToCamMat_list.Add(WorldToCamMat_all[id]);
                        EyeToWorldPos_list.Add(EyeToWorldPos_all[id]);

                        id++; // Update current index
                    }
                }

                // Get the index of the center camera
                int center_num = ChoiceCenterCamera(Ray_list, CamToWorldMat_list);
                // Reproject the points of regard
                List<Vector2> fix_gaze = ReprojectionPoRs(WorldToCamMat_list[center_num], EyeToWorldPos_list[center_num], fix_pos_list);
                // Calculate the dispersion
                float dispersion = CalculateDispersion(fix_gaze);

                // Slide the window if the dispersion is larger than the threshold
                while (dispersion > dispersion_th)
                {
                    // Remove the first element of the list
                    fix_pos_list.RemoveAt(0);
                    Ray_list.RemoveAt(0);
                    CamToWorldMat_list.RemoveAt(0);
                    WorldToCamMat_list.RemoveAt(0);
                    EyeToWorldPos_list.RemoveAt(0);

                    start++; // Uppdate the index of the fixation start

                    // End of the data
                    if (id >= (int)Ray_py.shape[0]) break;

                    // In case that the gaze direction is NaN
                    if (float.IsNaN(Ray_all[id].x) | float.IsNaN(Ray_all[id].y) | float.IsNaN(Ray_all[id].z))
                    {
                        break;
                    }
                    else
                    {
                        // Calculate the point of regard
                        Vector3 fix_pos = CalculatePoR(Ray_all[id], CamToWorldMat_all[id], EyeToWorldPos_all[id]);
                        fix_pos_list.Add(fix_pos);
                        Ray_list.Add(Ray_all[id]);
                        CamToWorldMat_list.Add(CamToWorldMat_all[id]);
                        WorldToCamMat_list.Add(WorldToCamMat_all[id]);
                        EyeToWorldPos_list.Add(EyeToWorldPos_all[id]);

                        // Get the index of the center camera
                        center_num = ChoiceCenterCamera(Ray_list, CamToWorldMat_list);
                        // Reproject the points of regard
                        fix_gaze = ReprojectionPoRs(WorldToCamMat_list[center_num], EyeToWorldPos_list[center_num], fix_pos_list);
                        // Calculate the dispersion
                        dispersion = CalculateDispersion(fix_gaze);

                        id++; // Update current index
                    }
                }

                // Expand the window if the duration is larger than the threshold
                while (dispersion <= dispersion_th)
                {
                    // End of the data
                    if (id >= (int)Ray_py.shape[0])
                    {
                        stop = id; // Uppdate the index of the fixation end
                        break;
                    }

                    // In case that the gaze direction is NaN
                    if (float.IsNaN(Ray_all[id].x) | float.IsNaN(Ray_all[id].y) | float.IsNaN(Ray_all[id].z))
                    {
                        stop = id; // Uppdate the index of the fixation end
                        break;
                    }
                    else
                    {
                        // Calculate the point of regard
                        Vector3 fix_pos = CalculatePoR(Ray_all[id], CamToWorldMat_all[id], EyeToWorldPos_all[id]);
                        fix_pos_list.Add(fix_pos);
                        Ray_list.Add(Ray_all[id]);
                        CamToWorldMat_list.Add(CamToWorldMat_all[id]);
                        WorldToCamMat_list.Add(WorldToCamMat_all[id]);
                        EyeToWorldPos_list.Add(EyeToWorldPos_all[id]);

                        // Get the index of the center camera
                        center_num = ChoiceCenterCamera(Ray_list, CamToWorldMat_list);
                        // Reproject the points of regard
                        fix_gaze = ReprojectionPoRs(WorldToCamMat_list[center_num], EyeToWorldPos_list[center_num], fix_pos_list);
                        // Calculate the dispersion
                        dispersion = CalculateDispersion(fix_gaze);

                        if (dispersion > dispersion_th)
                        {
                            break;
                        }
                        else
                        {
                            stop = id; // Uppdate the index of the fixation end
                            id++; // Update current index
                        }
                    }
                }

                // Add the window to the list
                if (stop-start >= duration_th)
                {
                    window.Add(new List<int> {start, stop});
                }

                // End of the data
                if (id >= (int)Ray_py.shape[0]) break;

                // Initialize the list
                fix_pos_list = new List<Vector3>(); // Points of regard
                Ray_list = new List<Vector3>();
                CamToWorldMat_list = new List<List<float>>(); // 3x3 Matrix
                WorldToCamMat_list = new List<List<float>>(); // 3x3 Matrix
                EyeToWorldPos_list = new List<Vector3>();
            }
        }
        PythonEngine.Shutdown();
    }

    public static void GetFixationByIVDT(string file, float deg_per_sec, float dispersion_th, out List<List<int>> window, int duration_th=10)
    {
        PythonEngine.Initialize(mode: ShutdownMode.Reload);

        using (Py.GIL()) // Get Global Interpreter Lock
        {
            PythonEngine.RunSimpleString($"import sys;sys.path.insert(1, '{python_src}');");
            dynamic mylib = Py.Import("extraction_fixation");
            dynamic Fix = mylib.ExtractFixation();

            Debug.Log("Data Loading");
            dynamic table = Fix.formatting(file);

            dynamic frame_rate = Fix.calculate_th();
            Debug.Log(frame_rate);

            float fs = (float)Fix.fs;
            float deg_per_frame = deg_per_sec / fs;

            dynamic Ray_py = Fix.ray; // Gaze direction
            dynamic CamToWorldMat_py = Fix.CamToWorldMat; // Camera (hmd or eye) to world rotation matrix (3x3 matrix)
            dynamic WorldToCamMat_py = Fix.WorldToCamMat; // World to camera (hmd or eye) rotation matrix (4x4 matrix)
            dynamic EyeToWorldPos_py = Fix.EyeToWorldPos; // Eye position in world coordinate system

            List<Vector3> Ray_all = new List<Vector3>();
            List<List<float>> CamToWorldMat_all = new List<List<float>>(); // 3x3 Matrix
            List<List<float>> WorldToCamMat_all = new List<List<float>>(); // 3x3 Matrix
            List<Vector3> EyeToWorldPos_all = new List<Vector3>();
            for (int i=0; i<(int)Ray_py.shape[0]; i++)
            {
                Ray_all.Add(new Vector3((float)Ray_py[i][0], (float)Ray_py[i][1], (float)Ray_py[i][2]));
                CamToWorldMat_all.Add(new List<float> {(float)CamToWorldMat_py[i][0][0], (float)CamToWorldMat_py[i][0][1], (float)CamToWorldMat_py[i][0][2],
                                                        (float)CamToWorldMat_py[i][1][0], (float)CamToWorldMat_py[i][1][1], (float)CamToWorldMat_py[i][1][2],
                                                        (float)CamToWorldMat_py[i][2][0], (float)CamToWorldMat_py[i][2][1], (float)CamToWorldMat_py[i][2][2]});
                WorldToCamMat_all.Add(new List<float> {(float)WorldToCamMat_py[i][0][0], (float)WorldToCamMat_py[i][0][1], (float)WorldToCamMat_py[i][0][2],
                                                        (float)WorldToCamMat_py[i][1][0], (float)WorldToCamMat_py[i][1][1], (float)WorldToCamMat_py[i][1][2],
                                                        (float)WorldToCamMat_py[i][2][0], (float)WorldToCamMat_py[i][2][1], (float)WorldToCamMat_py[i][2][2]});
                EyeToWorldPos_all.Add(new Vector3((float)EyeToWorldPos_py[i][0], (float)EyeToWorldPos_py[i][1], (float)EyeToWorldPos_py[i][2]));
            }

            // I-VDT
            int id = 0;
            window = new List<List<int>>();
            List<Vector3> fix_pos_list = new List<Vector3>(); // Points of regard
            List<Vector3> Ray_list = new List<Vector3>();
            List<List<float>> CamToWorldMat_list = new List<List<float>>(); // 3x3 matrix
            List<List<float>> WorldToCamMat_list = new List<List<float>>(); // 3x3 matrix
            List<Vector3> EyeToWorldPos_list = new List<Vector3>();
            while (id + duration_th < (int)Ray_py.shape[0])
            {
                int start = id; // Index of the fixation start
                int stop = id; // Index of the fixation end

                // Check the PoRs in the initial window
                while (id - start < duration_th)
                {
                    // End of the data
                    if (id >= (int)Ray_py.shape[0]) break;

                    // In case that the gaze direction is NaN
                    if (float.IsNaN(Ray_all[id].x) | float.IsNaN(Ray_all[id].y) | float.IsNaN(Ray_all[id].z))
                    {
                        // Initialize the list
                        fix_pos_list = new List<Vector3>(); // Points of regard
                        Ray_list = new List<Vector3>();
                        CamToWorldMat_list = new List<List<float>>(); // 3x3 matrix
                        WorldToCamMat_list = new List<List<float>>(); // 3x3 matrix
                        EyeToWorldPos_list = new List<Vector3>();

                        id++; // Update current index
                        start = id; // Uppdate the index of the fixation start
                    }
                    else
                    {
                        // Calculate angle between two gaze directions
                        float velocity = 0.0f;
                        if (fix_pos_list.Count > 0)
                        {
                            velocity = Vector3.Angle(Ray_all[id-1], Ray_all[id]);
                        }

                        if (velocity < deg_per_frame)
                        {
                            // Calculate the point of regard
                            Vector3 fix_pos = CalculatePoR(Ray_all[id], CamToWorldMat_all[id], EyeToWorldPos_all[id]);
                            fix_pos_list.Add(fix_pos);
                            Ray_list.Add(Ray_all[id]);
                            CamToWorldMat_list.Add(CamToWorldMat_all[id]);
                            WorldToCamMat_list.Add(WorldToCamMat_all[id]);
                            EyeToWorldPos_list.Add(EyeToWorldPos_all[id]);

                            id++; // Update current index
                        }
                        else
                        {
                            // Initialize the list
                            fix_pos_list = new List<Vector3>(); // Points of regard
                            Ray_list = new List<Vector3>();
                            CamToWorldMat_list = new List<List<float>>(); // 3x3 matrix
                            WorldToCamMat_list = new List<List<float>>(); // 3x3 matrix
                            EyeToWorldPos_list = new List<Vector3>();

                            id++; // Update current index
                            start = id; // Uppdate the index of the fixation start
                        }

                    }
                }

                // Get the index of the center camera
                int center_num = ChoiceCenterCamera(Ray_list, CamToWorldMat_list);
                // Reproject the points of regard
                List<Vector2> fix_gaze = ReprojectionPoRs(WorldToCamMat_list[center_num], EyeToWorldPos_list[center_num], fix_pos_list);
                // Calculate the dispersion
                float dispersion = CalculateDispersion(fix_gaze);

                // Slide the window if the dispersion is larger than the threshold
                while (dispersion > dispersion_th)
                {
                    // Remove the first element of the list
                    fix_pos_list.RemoveAt(0);
                    Ray_list.RemoveAt(0);
                    CamToWorldMat_list.RemoveAt(0);
                    WorldToCamMat_list.RemoveAt(0);
                    EyeToWorldPos_list.RemoveAt(0);

                    start++; // Uppdate the index of the fixation start

                    // End of the data
                    if (id >= (int)Ray_py.shape[0]) break;

                    // In case that the gaze direction is NaN
                    if (float.IsNaN(Ray_all[id].x) | float.IsNaN(Ray_all[id].y) | float.IsNaN(Ray_all[id].z))
                    {
                        break;
                    }
                    else
                    {
                        // Calculate angle between two gaze directions
                        float velocity = 0.0f;
                        if (fix_pos_list.Count > 0)
                        {
                            velocity = Vector3.Angle(Ray_all[id-1], Ray_all[id]);
                        }

                        // Check the velocity threshold
                        if (velocity < deg_per_frame)
                        {
                            // Calculate the point of regard
                            Vector3 fix_pos = CalculatePoR(Ray_all[id], CamToWorldMat_all[id], EyeToWorldPos_all[id]);
                            fix_pos_list.Add(fix_pos);
                            Ray_list.Add(Ray_all[id]);
                            CamToWorldMat_list.Add(CamToWorldMat_all[id]);
                            WorldToCamMat_list.Add(WorldToCamMat_all[id]);
                            EyeToWorldPos_list.Add(EyeToWorldPos_all[id]);
                            // Get the index of the center camera
                            center_num = ChoiceCenterCamera(Ray_list, CamToWorldMat_list);
                            // Reproject the points of regard
                            fix_gaze = ReprojectionPoRs(WorldToCamMat_list[center_num], EyeToWorldPos_list[center_num], fix_pos_list);
                            // Calculate the dispersion
                            dispersion = CalculateDispersion(fix_gaze);

                            id++; // Update current index
                        }
                        else
                        {
                            id++; // Update current index
                        }
                    }
                }

                // Expand the window if the duration is larger than the threshold
                while (dispersion <= dispersion_th)
                {
                    // End of the data
                    if (id >= (int)Ray_py.shape[0])
                    {
                        stop = id; // Uppdate the index of the fixation end
                        break;
                    }

                    // In case that the gaze direction is NaN
                    if (float.IsNaN(Ray_all[id].x) | float.IsNaN(Ray_all[id].y) | float.IsNaN(Ray_all[id].z))
                    {
                        stop = id; // Uppdate the index of the fixation end
                        break;
                    }
                    else
                    {
                        // Calculate angle between two gaze directions
                        float velocity = 0.0f;
                        if (fix_pos_list.Count>0)
                        {
                            velocity = Vector3.Angle(Ray_all[id-1], Ray_all[id]);
                        }

                        // Check the velocity threshold
                        if (velocity < deg_per_frame)
                        {
                            // Calculate the point of regard
                            Vector3 fix_pos = CalculatePoR(Ray_all[id], CamToWorldMat_all[id], EyeToWorldPos_all[id]);
                            fix_pos_list.Add(fix_pos);
                            Ray_list.Add(Ray_all[id]);
                            CamToWorldMat_list.Add(CamToWorldMat_all[id]);
                            WorldToCamMat_list.Add(WorldToCamMat_all[id]);
                            EyeToWorldPos_list.Add(EyeToWorldPos_all[id]);
                            // Get the index of the center camera
                            center_num = ChoiceCenterCamera(Ray_list, CamToWorldMat_list);
                            // Reproject the points of regard
                            fix_gaze = ReprojectionPoRs(WorldToCamMat_list[center_num], EyeToWorldPos_list[center_num], fix_pos_list);
                            // Calculate the dispersion
                            dispersion = CalculateDispersion(fix_gaze);

                            stop = id; // Uppdate the index of the fixation end
                            id++; // Update current index
                        }
                        else
                        {
                            stop = id; // Uppdate the index of the fixation end
                            id++; // Update current index
                            break;
                        }
                    }
                }

                // Add the window to the list
                if (stop-start >= duration_th)
                {
                    window.Add(new List<int> {start, stop});
                }

                // End of the data
                if (id >= (int)Ray_py.shape[0]) break;

                // Initialize the list
                fix_pos_list = new List<Vector3>(); // Points of regard
                Ray_list = new List<Vector3>();
                CamToWorldMat_list = new List<List<float>>(); // 3x3 matrix
                WorldToCamMat_list = new List<List<float>>(); // 3x3 matrix
                EyeToWorldPos_list = new List<Vector3>();
            }
        }
        PythonEngine.Shutdown();
    }

    public static float CalculateDispersion(List<Vector2> fix_gaze)
    {
        List<float> fix_dis = new List<float>();
        Vector2 fix_mean = new Vector2();
        for (int i=0; i<fix_gaze.Count; i++)
        {
            fix_mean += fix_gaze[i];
        }
        fix_mean /= fix_gaze.Count;

        for (int i=0; i<fix_gaze.Count; i++)
        {
            float dis = (fix_gaze[i] - fix_mean).sqrMagnitude;
            fix_dis.Add(dis);
        }

        float dispersion = fix_dis.Max();
        return dispersion;
    }

    public static Vector3 CalculatePoR(Vector3 ray, List<float> camtoworldmat, Vector3 eyetoworldpos)
    {
        // Convert the gaze direction to the world coordinate system
        Vector3 direction = new Vector3(ray.x*camtoworldmat[0] + ray.y*camtoworldmat[1] + ray.z*camtoworldmat[2],
                                        ray.x*camtoworldmat[3] + ray.y*camtoworldmat[4] + ray.z*camtoworldmat[5],
                                        ray.x*camtoworldmat[6] + ray.y*camtoworldmat[7] + ray.z*camtoworldmat[8]);
        // Raycast from the eye position
        RaycastHit hitInfo;
        Physics.Raycast(eyetoworldpos, direction, out hitInfo, 100f);
        return hitInfo.point;
    }

     public static List<Vector3> CalculatePoRs(List<Vector3> ray, List<List<float>> camtoworldmat, List<Vector3> eyetoworldpos) // PoRを計算
    {
        List<Vector3> fix_pos = new List<Vector3>();
        for (int i=0; i<ray.Count; i++)
        {
            // Convert the gaze direction to the world coordinate system
            List<float> mat = camtoworldmat[i];
            Vector3 WorldDirection = new Vector3(ray[i].x*mat[0] + ray[i].y*mat[1] + ray[i].z*mat[2],
                                                ray[i].x*mat[3] + ray[i].y*mat[4] + ray[i].z*mat[5],
                                                ray[i].x*mat[6] + ray[i].y*mat[7] + ray[i].z*mat[8]); // 視線をWorld座標に変換

            // Raycast from the eye position
            RaycastHit hitInfo;
            Physics.Raycast(eyetoworldpos[i], WorldDirection, out hitInfo, 100f);
            fix_pos.Add(hitInfo.point);
        }
        return fix_pos;
    }

    public static int ChoiceCenterCamera(List<Vector3> ray, List<List<float>> camtoworldmat)
    {
        int index = -1;
        Vector3 ray_mean = new Vector3(0.0f, 0.0f, 0.0f);
        List<Vector3> WorldDirectionList = new List<Vector3>();
        for (int i=0; i<ray.Count; i++)
        {
            // Convert the gaze direction to the world coordinate system
            List<float> mat = camtoworldmat[i];
            Vector3 WorldDirection = new Vector3(ray[i].x*mat[0] + ray[i].y*mat[1] + ray[i].z*mat[2],
                                                ray[i].x*mat[3] + ray[i].y*mat[4] + ray[i].z*mat[5],
                                                ray[i].x*mat[6] + ray[i].y*mat[7] + ray[i].z*mat[8]);

            ray_mean += WorldDirection;
            WorldDirectionList.Add(WorldDirection);
        }
        ray_mean /= ray.Count;
        float value = float.MaxValue;
        for (int i=0; i<ray.Count; i++)
        {
            float d = (ray_mean - WorldDirectionList[i]).magnitude;
            if (d < value)
            {
                value = d;
                index = i;
            }
        }
        return index;
    }

    public static List<Vector2> ReprojectionPoRs(List<float> worldtocammat, Vector3 eyetoworldpos, List<Vector3> fix_pos_list)
    {
        List<Vector2> fix_gaze = new List<Vector2>();
        for (int i=0; i<fix_pos_list.Count; i++)
        {
            // Convert the gaze direction to the camera coordinate system
            Vector3 ray_world = new Vector3(fix_pos_list[i].x-eyetoworldpos.x, fix_pos_list[i].y-eyetoworldpos.y, fix_pos_list[i].z-eyetoworldpos.z);
            Vector3 ray_eye = new Vector3(ray_world.x*worldtocammat[0] + ray_world.y*worldtocammat[1] + ray_world.z*worldtocammat[2],
                                        ray_world.x*worldtocammat[3] + ray_world.y*worldtocammat[4] + ray_world.z*worldtocammat[5],
                                        ray_world.x*worldtocammat[6] + ray_world.y*worldtocammat[7] + ray_world.z*worldtocammat[8]);

            // Convert the gaze direction on scene image plane
            float xh = ray_eye.x / ray_eye.z;
            float yh = ray_eye.y / ray_eye.z;
            Vector2 gaze = new Vector2(xh, yh);
            fix_gaze.Add(gaze);
        }
        return fix_gaze;
    }

    public static List<Vector3> CalibrateRayBy3D(float[] param, List<Vector3> ray)
    {
        // Convert the angle to radian
        float alpha = param[0] * Mathf.Deg2Rad;
        float beta = param[1] * Mathf.Deg2Rad;
        // Rotation matrix
        List<float> rot = new List<float> {Mathf.Cos(alpha), -Mathf.Sin(alpha)*Mathf.Sin(beta), Mathf.Sin(alpha)*Mathf.Cos(beta),
                                            0.0f, Mathf.Cos(beta), Mathf.Sin(beta),
                                            -Mathf.Sin(alpha), -Mathf.Cos(alpha)*Mathf.Sin(beta), Mathf.Cos(alpha)*Mathf.Cos(beta)};

        // Calibrate the gaze direction
        List<Vector3> calib_ray_list = new List<Vector3>();
        for (int i=0; i<ray.Count; i++)
        {
            Vector3 calib_ray = new Vector3(ray[i].x*rot[0] + ray[i].y*rot[1] + ray[i].z*rot[2],
                                            ray[i].x*rot[3] + ray[i].y*rot[4] + ray[i].z*rot[5],
                                            ray[i].x*rot[6] + ray[i].y*rot[7] + ray[i].z*rot[8]);
            calib_ray = calib_ray.normalized;
            calib_ray_list.Add(calib_ray);
        }
        return calib_ray_list;
    }

    public static float CalculateReprojectionError(List<Vector2> fix_gaze, out Vector2 fix_mean)
    {
        // Calculate the mean of the points of regard
        fix_mean = new Vector2();
        for (int i=0; i<fix_gaze.Count; i++)
        {
            fix_mean += fix_gaze[i];
        }
        fix_mean /= fix_gaze.Count;

        // Calculate the reprojection error
        float repro_error = 0.0f;
        for (int j=0; j<fix_gaze.Count; j++)
        {
            repro_error += (fix_gaze[j].x-fix_mean.x)*(fix_gaze[j].x-fix_mean.x) + (fix_gaze[j].y-fix_mean.y)*(fix_gaze[j].y-fix_mean.y);
        }
        repro_error /= fix_gaze.Count;
        return repro_error;
    }

    public static void ShowListContentsInTheDebugLog<T>(List<T> list)
    {
        string log = "";
        foreach(var content in list.Select((val, idx) => new {val, idx}))
        {
            if (content.idx == list.Count - 1)
                log += content.val.ToString();
            else
                log += content.val.ToString() + ", ";
        }
        Debug.Log(log);
    }
}