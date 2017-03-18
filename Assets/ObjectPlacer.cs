using System;
using System.Collections.Generic;
using HoloToolkit.Unity;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    public bool DrawDebugBoxes = true;

    public SpatialUnderstandingCustomMesh SpatialUnderstandingMesh;

    [Tooltip("The desired size of cubes in the world.")]
    public Vector3 GuitarSize = new Vector3(1f, 1f, 1f);

    public GameObject GuitarPrefab;

    private readonly List<BoxDrawer.Box> _lineBoxList = new List<BoxDrawer.Box>();

    private readonly Queue<PlacementResult> _results = new Queue<PlacementResult>();

    private bool _timeToHideMesh;
    private BoxDrawer _boxDrawing;

    // Use this for initialization
    void Start()
    {
        if (DrawDebugBoxes)
        {
            _boxDrawing = new BoxDrawer(gameObject);
        }

    }

    // Update is called once per frame
    void Update()
    {
        ProcessPlacementResults();

        if (_timeToHideMesh)
        {
            HideGridEnableOcclulsion();
            _timeToHideMesh = false;
        }

        if (DrawDebugBoxes)
        {
            _boxDrawing.UpdateBoxes(_lineBoxList);
        }

    }

    private void HideGridEnableOcclulsion()
    {
        SpatialUnderstandingMesh.DrawProcessedMesh = false;
    }

    public void CreateScene()
    {
        Debug.Log("Create scene");
        // Only if we're enabled
        if (!SpatialUnderstanding.Instance.AllowSpatialUnderstanding)
        {
            return;
        }

        SpatialUnderstandingDllObjectPlacement.Solver_Init();

        Debug.Log("Generating World");

        List<PlacementQuery> queries = CreateLocationQueriesForSolver(1, GuitarSize, ObjectType.Cube);

        GetLocationsFromSolver(queries);

    }

    private void ProcessPlacementResults()
    {
        if (_results.Count > 0)
        {
            var toPlace = _results.Dequeue();
            // Output
            if (DrawDebugBoxes)
            {
                DrawBox(toPlace, Color.red);
            }

            var rotation = Quaternion.LookRotation(toPlace.Normal, Vector3.up);

            switch (toPlace.ObjType)
            {
                case ObjectType.Cube:
                    CreateWideBuilding(toPlace.Position, rotation);
                    break;
            }
        }
    }

    public void CreateWideBuilding(Vector3 positionCenter, Quaternion rotation)
    {
        // Stay center in the square but move down to the ground
        var position = positionCenter - new Vector3(0, 0, 0);//CubeSize.y * .5f, 0);

        GameObject newObject = Instantiate(GuitarPrefab, position, GuitarPrefab.transform.rotation) as GameObject;

        if (newObject != null)
        {
            // Set the parent of the new object the GameObject it was placed on
            newObject.transform.parent = gameObject.transform;

            newObject.transform.localScale = RescaleToDesiredSizeProportional(GuitarPrefab, GuitarSize);
        }
    }

    private Vector3 RescaleToDesiredSizeProportional(GameObject objectToScale, Vector3 desiredSize)
    {
        float scaleFactor = CalcScaleFactorHelper(new List<GameObject> { objectToScale }, desiredSize);

        return objectToScale.transform.localScale * scaleFactor;
    }

    private float CalcScaleFactorHelper(List<GameObject> objects, Vector3 desiredSize)
    {
        float maxScale = float.MaxValue;

        foreach (var obj in objects)
        {
            var curBounds = GetBoundsForAllChildren(obj).size;
            var difference = curBounds - desiredSize;

            float ratio;

            if (difference.x > difference.y && difference.x > difference.z)
            {
                ratio = desiredSize.x / curBounds.x;
            }
            else if (difference.y > difference.x && difference.y > difference.z)
            {
                ratio = desiredSize.y / curBounds.y;
            }
            else
            {
                ratio = desiredSize.z / curBounds.z;
            }

            if (ratio < maxScale)
            {
                maxScale = ratio;
            }
        }

        return maxScale;
    }

    /// <summary>
    /// Get the bounding box that encapsulates the bounds for all children of a parent
    /// </summary>
    /// <param name="parentObject">parent object</param>
    /// <returns></returns>
    private Bounds GetBoundsForAllChildren(GameObject parentObject)
    {
        Bounds result = new Bounds(Vector3.zero, Vector3.zero);

        foreach (var renderer in parentObject.GetComponentsInChildren<Renderer>())
        {
            if (result.extents == Vector3.zero)
            {
                result = renderer.bounds;
            }
            else
            {
                // grow the total bounding box to encapsulate the child's bounding box
                result.Encapsulate(renderer.bounds);
            }
        }

        return result;
    }

    private void DrawBox(PlacementResult boxLocation, Color color)
    {
        if (boxLocation != null)
        {
            _lineBoxList.Add(
                new BoxDrawer.Box(
                    boxLocation.Position,
                    Quaternion.LookRotation(boxLocation.Normal, Vector3.up),
                    color,
                    boxLocation.Dimensions * 0.5f)
                );
        }
    }

    private void GetLocationsFromSolver(List<PlacementQuery> placementQueries)
    {
#if UNITY_WSA && !UNITY_EDITOR
        System.Threading.Tasks.Task.Run(() =>
        {
            // Go through the queries in the list
            for (int i = 0; i < placementQueries.Count; ++i)
            {
                var result = PlaceObject(placementQueries[i].ObjType.ToString() + i,
                                         placementQueries[i].PlacementDefinition,
                                         placementQueries[i].Dimensions,
                                         placementQueries[i].ObjType,
                                         placementQueries[i].PlacementRules,
                                         placementQueries[i].PlacementConstraints);
                if (result != null)
                {
                    _results.Enqueue(result);
                }
            }

            _timeToHideMesh = true;
        });
#else
        _timeToHideMesh = true;
#endif
    }

    private PlacementResult PlaceObject(string placementName,
    SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition placementDefinition,
    Vector3 boxFullDims,
    ObjectType objType,
    List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule> placementRules = null,
    List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint> placementConstraints = null)
    {

        // New query
        if (SpatialUnderstandingDllObjectPlacement.Solver_PlaceObject(
            placementName,
            SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(placementDefinition),
            (placementRules != null) ? placementRules.Count : 0,
            ((placementRules != null) && (placementRules.Count > 0)) ? SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(placementRules.ToArray()) : IntPtr.Zero,
            (placementConstraints != null) ? placementConstraints.Count : 0,
            ((placementConstraints != null) && (placementConstraints.Count > 0)) ? SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(placementConstraints.ToArray()) : IntPtr.Zero,
            SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticObjectPlacementResultPtr()) > 0)
        {
            SpatialUnderstandingDllObjectPlacement.ObjectPlacementResult placementResult = SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticObjectPlacementResult();

            return new PlacementResult(placementResult.Clone() as SpatialUnderstandingDllObjectPlacement.ObjectPlacementResult, boxFullDims, objType);
        }

        return null;
    }

    private List<PlacementQuery> CreateLocationQueriesForSolver(int desiredLocationCount, Vector3 boxFullDims, ObjectType objType)
    {
        List<PlacementQuery> placementQueries = new List<PlacementQuery>();

        var halfBoxDims = boxFullDims * .5f;

        var disctanceFromOtherObjects = halfBoxDims.x > halfBoxDims.z ? halfBoxDims.x * 3f : halfBoxDims.z * 3f;

        for (int i = 0; i < desiredLocationCount; ++i)
        {
            var placementRules = new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule>
            {
                SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromOtherObjects(disctanceFromOtherObjects)
            };

            var placementConstraints = new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint>();

            SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition placementDefinition = SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_OnFloor(halfBoxDims);

            placementQueries.Add(
                new PlacementQuery(placementDefinition,
                                   boxFullDims,
                                   objType,
                                   placementRules,
                                   placementConstraints
                                    ));
        }

        return placementQueries;
    }

}