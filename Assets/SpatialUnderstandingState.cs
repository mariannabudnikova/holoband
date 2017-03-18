using System;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;

public class SpatialUnderstandingState : MonoBehaviour
{
    private bool m_isInitialized;
    public float kMinAreaForComplete = 50.0f;
    public float kMinHorizAreaForComplete = 25.0f;
    public float kMinWallAreaForComplete = 10.0f;

    private bool _triggered;
    public ObjectPlacer Placer;

    // Update is called once per frame
    private void Update()
    {
        // check if enough of the room is scanned
        if (!m_isInitialized && DoesScanMeetMinBarForCompletion)
        {
            // let service know we're done scanning
            SpatialUnderstanding.Instance.RequestFinishScan();
            m_isInitialized = true;
        }

        // place the quitars once the scanning is complete
        if (!_triggered && SpatialUnderstanding.Instance.ScanState == SpatialUnderstanding.ScanStates.Done)
        {
            _triggered = true;
            Placer.CreateScene();
        }
    }

    public bool DoesScanMeetMinBarForCompletion
    {
        get
        {
            // Only allow this when we are actually scanning
            if ((SpatialUnderstanding.Instance.ScanState != SpatialUnderstanding.ScanStates.Scanning) ||
                (!SpatialUnderstanding.Instance.AllowSpatialUnderstanding))
            {
                return false;
            }

            // Query the current playspace stats
            IntPtr statsPtr = SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticPlayspaceStatsPtr();
            if (SpatialUnderstandingDll.Imports.QueryPlayspaceStats(statsPtr) == 0)
            {
                return false;
            }
            SpatialUnderstandingDll.Imports.PlayspaceStats stats = SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticPlayspaceStats();

            // Check our preset requirements
            if ((stats.TotalSurfaceArea > kMinAreaForComplete) ||
                (stats.HorizSurfaceArea > kMinHorizAreaForComplete) ||
                (stats.WallSurfaceArea > kMinWallAreaForComplete))
            {
                return true;
            }
            return false;
        }
    }
}