using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Python.Runtime;

public class OptimizeDE : MonoBehaviour
{
    StreamWriter sw;
    public bool office_flag; // Flag indicating office environment or not
    public bool calib_flag; // Flag indicating whether to calibrate or not
    public bool raw_flag; // Flag indicating whether to start from the initial prameter or the baseline parameter
    public bool ivt_flag; // Flag indicating whether to use I-VT or not
    public bool idt_flag; // Flag indicating whether to use I-DT or not
    public string param_result_path; // Path where the optimized parameters are saved
    public string gaze_data_path; // Path where the user's gaze data is saved
    List<int> user_list = new List<int>{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17}; // List of user numbers
    List<int> remove_list = new List<int>(); // List of Fixation to be removed
    float dispersion_th = 0.00015f; // Dispersion threshold (0.7deg)
    float velocity_th = 80.0f; // Velocity threshold (deg/s)
    int duration_th = 8; // Duration threshold (one frame is 20ms)
    float[] param_est; // Estimated parameters
    List<List<Vector3>> BaseRay; // Gaze direction at the visaual axis parameter
    List<List<List<float>>> CamToWorldMat; // Camera (hmd or eye) to world rotation matrix
    List<List<List<float>>> WorldToCamMat; // World to camera (hmd or eye) rotation matrix
    List<List<Vector3>> EyeToWorldPos; // Eye position in world coordinate system
    List<float> EyePosMove; // List of eye movement
    List<int> fix_num_list; // List of representative cameras
    delegate float CallBackProc(float[] param);

     public int Optimize(float[] func_param, List<List<List<Vector3>>> BaseRay_lis, List<List<List<List<float>>>> CamToWorldMat_lis, List<List<List<List<float>>>> WorldToCamMat_lis, List<List<List<Vector3>>> EyeToWorldPos_lis, List<List<float>> EyePosMove_lis)
    {
        PythonEngine.Initialize(mode: ShutdownMode.Reload);

        using (Py.GIL()) // Get Global Interpreter Lock
        {
            dynamic np = Py.Import("numpy");
            dynamic opt = Py.Import("scipy.optimize");
            dynamic js = Py.Import("json");

            // Create a callback function
            CallBackProc callBack = new CallBackProc(ErrorFunc);

            List<float> param = new List<float>();
            param.AddRange(func_param);

            for (int f=0; f<18; f++)
            {
                // Skip when not matching list elements
                if (user_list.Contains(f) == false)
                {
                    continue;
                }
                Debug.Log($"user{f}");

                BaseRay = BaseRay_lis[f];
                CamToWorldMat = CamToWorldMat_lis[f];
                WorldToCamMat = WorldToCamMat_lis[f];
                EyeToWorldPos = EyeToWorldPos_lis[f];
                EyePosMove = EyePosMove_lis[f];

                // Caluculate the fixation to be removed
                remove_list = new List<int>();
                remove_list = RemoveFixation(func_param);
                Debug.Log("remove_list.Count: " + remove_list.Count);

                // Display the initial error
                float Error_ini = ErrorFunc(func_param);
                Debug.Log("Initial Error: " + Error_ini);

                // Start optimization
                float start = Time.realtimeSinceStartup;

                int div = 4;
                List<float[]> param_est_lis = new List<float[]>();
                List<float> value_est_lis = new List<float>();
                for (int i=0; i<div; i++)
                {
                    for (int j=0; j<div; j++)
                    {
                        List<float> lb = new List<float>();
                        List<float> ub = new List<float>();
                        float[] lb_param = {-5.0f + 10.0f/div*i, -5.0f + 10.0f/div*j};
                        float[] ub_param = {-5.0f + 10.0f/div*(i+1), -5.0f + 10.0f/div*(j+1)};
                        lb.AddRange(lb_param);
                        ub.AddRange(ub_param);
                        dynamic bounds = opt.Bounds(lb, ub);
                        dynamic result = opt.differential_evolution(callBack, bounds, popsize: 15, tol: 0.0001, seed: 1);

                        param_est_lis.Add(new float[] {(float)result["x"][0], (float)result["x"][1]});
                        value_est_lis.Add((float)result["fun"]);
                    }
                }
                // Get the optimal parameters
                int ind_opt = value_est_lis.Select((x, i) => new { x, i }).Aggregate((min, xi) => xi.x < min.x ? xi : min).i;;
                param_est = param_est_lis[ind_opt];
                Debug.Log("optimization result");
                Debug.Log(param_est[0] +  "," + param_est[1] + "," + value_est_lis[ind_opt]);

                // End optimization
                float stop = Time.realtimeSinceStartup;
                Debug.Log("optimize time: " + (stop-start));

                // Calculate the evaluation value
                float Error = ErrorFunc(param_est);
                float Error_mean = Error / BaseRay.Count;

                // Output csv file
                string param_est_str = string.Join(",", param_est);
                sw.WriteLine(stop-start + "," + param_est_str + "," + Error + "," + Error_mean);
                sw.Flush();
            }
        }
        PythonEngine.Shutdown();
        return 0;
    }

    public float ErrorFunc(float[] param)
    {
        float ReproError = 0.0f;
        for (int i=0; i<BaseRay.Count; i++)
        {
            // Skip when not matching list elements
            if (remove_list.Contains(i) == true)
            {
                continue;
            }

            List<Vector3> ray = BaseRay[i];
            List<List<float>> camtoworldmat = CamToWorldMat[i];
            List<List<float>> worldtocammat = WorldToCamMat[i];
            List<Vector3> eyetoworldpos = EyeToWorldPos[i];

            // Caluculate the calibration gaze direction
            List<Vector3> calib_ray = OptimizeUtil.CalibrateRayBy3D(param, ray);

            // Caluculate the points of regard
            List<Vector3> fix_pos_list = OptimizeUtil.CalculatePoRs(calib_ray, camtoworldmat, eyetoworldpos);

            // In case of non-collision gaze direction
            int zero_count = fix_pos_list.Count(value => value == new Vector3(0.0f, 0.0f, 0.0f));
            if (zero_count != 0)
            {
                Debug.Log("Fixation" + i + " skip");
                continue;
            }

            // Reproject PoR to the representative camera
            int num = fix_num_list[i];
            List<Vector2> fix_gaze = OptimizeUtil.ReprojectionPoRs(worldtocammat[num], eyetoworldpos[num], fix_pos_list);

            // Calculate the reprojection error
            Vector2 fix_mean = new Vector2();
            float repro_error = OptimizeUtil.CalculateReprojectionError(fix_gaze, out fix_mean);
            ReproError += repro_error;
        }
        return ReproError;
    }

    public List<int> RemoveFixation(float [] param)
    {
        fix_num_list = new List<int>();

        for (int i=0; i<BaseRay.Count; i++)
        {
            List<Vector3> ray = BaseRay[i];
            List<List<float>> camtoworldmat = CamToWorldMat[i];
            List<List<float>> worldtocammat = WorldToCamMat[i];
            List<Vector3> eyetoworldpos = EyeToWorldPos[i];

            List<Vector3> calib_ray = OptimizeUtil.CalibrateRayBy3D(param, ray);

            // Caluculate the points of regard
            List<Vector3> fix_pos_list = OptimizeUtil.CalculatePoRs(calib_ray, camtoworldmat, eyetoworldpos);

            // In case of non-collision gaze direction
            int zero_count = fix_pos_list.Count(value => value == new Vector3(0.0f, 0.0f, 0.0f));
            if (zero_count != 0)
            {
                Debug.Log("Fixation" + i + " skip");
                remove_list.Add(i);
                continue;
            }
        }

        return remove_list;
    }

    void Start()
    {
        string scene;
        if (office_flag)
        {
            scene = "office";
        }
        else
        {
            scene = "supermarket";
        }

        if (calib_flag)
        {
            sw = new StreamWriter(param_result_path + $"param_est_{scene}.csv", true);
        }
        else
        {
            sw = new StreamWriter(param_result_path + $"param_ini_{scene}.csv", true);
        }

        string[] header_array = {"time", "alpha", "beta", "value", "repro"};
        string header = string.Join(",", header_array);
        sw.WriteLine(header);
        string[] files = new string[]{gaze_data_path + $"gaze_user1_{scene}.csv",
                                    gaze_data_path + $"gaze_user5_{scene}.csv",
                                    gaze_data_path + $"gaze_user6_{scene}.csv",
                                    gaze_data_path + $"gaze_user7_{scene}.csv",
                                    gaze_data_path + $"gaze_user9_{scene}.csv",
                                    gaze_data_path + $"gaze_user10_{scene}.csv",
                                    gaze_data_path + $"gaze_user11_{scene}.csv",
                                    gaze_data_path + $"gaze_user12_{scene}.csv",
                                    gaze_data_path + $"gaze_user13_{scene}.csv",
                                    gaze_data_path + $"gaze_user14_{scene}.csv",
                                    gaze_data_path + $"gaze_user15_{scene}.csv",
                                    gaze_data_path + $"gaze_user16_{scene}.csv",
                                    gaze_data_path + $"gaze_user17_{scene}.csv",
                                    gaze_data_path + $"gaze_user18_{scene}.csv",
                                    gaze_data_path + $"gaze_user19_{scene}.csv",
                                    gaze_data_path + $"gaze_user20_{scene}.csv",
                                    gaze_data_path + $"gaze_user21_{scene}.csv",
                                    gaze_data_path + $"gaze_user22_{scene}.csv"};

        OptimizeUtil.PySetting();

        List<List<List<Vector3>>> BaseRay_lis = new List<List<List<Vector3>>>();
        List<List<List<List<float>>>> CamToWorldMat_lis = new List<List<List<List<float>>>>();
        List<List<List<List<float>>>> WorldToCamMat_lis = new List<List<List<List<float>>>>();
        List<List<List<Vector3>>> EyeToWorldPos_lis = new List<List<List<Vector3>>>();
        List<List<float>> EyePosMove_lis = new List<List<float>>();

        for (int f=0; f<18; f++)
        {
            // Skip when not matching list elements
            if (user_list.Contains(f) == false)
            {
                BaseRay_lis.Add(new List<List<Vector3>>());
                CamToWorldMat_lis.Add(new List<List<List<float>>>());
                WorldToCamMat_lis.Add(new List<List<List<float>>>());
                EyeToWorldPos_lis.Add(new List<List<Vector3>>());
                EyePosMove_lis.Add(new List<float>());
                continue;
            }
            Debug.Log($"user{f}");

            if (ivt_flag) // I-VT
            {
                OptimizeUtil.ExtractFixationByIVT(files[f], out  BaseRay, out  CamToWorldMat, out WorldToCamMat, out EyeToWorldPos, out EyePosMove, duration_th, velocity_th, true);
                Debug.Log($"Number of fixation: {BaseRay.Count}");
            }
            else if (idt_flag) // I-DT
            {
                List<List<int>> window = new List<List<int>>();
                OptimizeUtil.GetFixationByIDT(files[f], dispersion_th, out window, duration_th);
                OptimizeUtil.ExtractFixation(files[f],  window, out  BaseRay, out  CamToWorldMat, out WorldToCamMat, out EyeToWorldPos, out EyePosMove);
                Debug.Log($"Number of fixation: {BaseRay.Count}");
            }
            else // I-VDT
            {
                List<List<int>> window = new List<List<int>>();
                OptimizeUtil.GetFixationByIVDT(files[f], velocity_th, dispersion_th, out window, duration_th);
                OptimizeUtil.ExtractFixation(files[f],  window, out  BaseRay, out  CamToWorldMat, out WorldToCamMat, out EyeToWorldPos, out EyePosMove);
                Debug.Log($"Number of fixation: {BaseRay.Count}");
            }

            BaseRay_lis.Add(BaseRay);
            CamToWorldMat_lis.Add(CamToWorldMat);
            WorldToCamMat_lis.Add(WorldToCamMat);
            EyeToWorldPos_lis.Add(EyeToWorldPos);
            EyePosMove_lis.Add(EyePosMove);
        }

        if (calib_flag) // Estimate the parameters
        {
            if (raw_flag) // Start from the optical axis parameters
            {
                float[] param_ini = {1.021f, 3.306f};
                Optimize(param_ini, BaseRay_lis, CamToWorldMat_lis, WorldToCamMat_lis, EyeToWorldPos_lis, EyePosMove_lis);
            }
            else // Start from the visual axis parameters
            {
                float[] param_ini = {0.0f, 0.0f};
                Optimize(param_ini, BaseRay_lis, CamToWorldMat_lis, WorldToCamMat_lis, EyeToWorldPos_lis, EyePosMove_lis);
            }
        }
        else // Caluculate the evaluation value
        {
            if (raw_flag) // Use the optical axis parameters
            {
                param_est = new float[] {-1.021f, -3.306f};
            }
            else // Use the visual axis parameters
            {
                param_est = new float[] {0.0f, 0.0f};
            }

            // Caluculate the fixation to be removed
            remove_list = new List<int>{};
            remove_list = RemoveFixation(param_est);
            Debug.Log("remove_list.Count: " + remove_list.Count);
            OptimizeUtil.ShowListContentsInTheDebugLog(remove_list);

            // Calculate the evaluation value
            float Error = ErrorFunc(param_est);
            float Error_mean = Error / BaseRay.Count;

            // Output csv file
            string param_est_str = string.Join(",", param_est);
            sw.WriteLine(0.0f + "," + param_est_str + "," + Error + "," + Error_mean);
            sw.Flush();
        }
    }
}