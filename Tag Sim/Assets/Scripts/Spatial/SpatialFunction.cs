using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// The type of spatial input to be used in a single layer of a spatial function.
/// </summary>
public enum SpatialInput
{
    TargetRange,
    PathDistance,
    LoS,
    AgentDistance,
    PathPrediction,
    None
}

/// <summary>
/// The spatial operation to be performed between two layers in a spatial function.
/// </summary>
public enum SpatialOp
{
    None,
    Add,
    Multiply
}

/// <summary>
/// A single layer in our spatial function. Layers are accumulated using spatial operations
/// to build a complete mapping of how our agents should navigate the world.
/// </summary>
[System.Serializable]
public class SpatialLayer
{
    [SerializeField] public SpatialInput InputType;
    [SerializeField] public SpatialOp Operation;
    [SerializeField] public AnimationCurve ResponseCurve;

    public SpatialLayer(SpatialInput inputType = SpatialInput.None, SpatialOp operation = SpatialOp.None)
    {
        InputType = inputType;
        Operation = operation;
        ResponseCurve = new AnimationCurve();
    }
}

/// <summary>
/// A spatial function is an editable curve that can be used to determine the spatial relationship between two objects.
/// It combines various distance-related inputs, such as line-of-site, distance, or path-distance, in order 
/// to rank the tiles of an AI agent's world perception.
/// </summary>
[CreateAssetMenu(menuName = "Spatial/SpatialFunction")]
public class SpatialFunction : ScriptableObject
{
    [SerializeField] public List<SpatialLayer> Layers;
    [SerializeField] public float LastCellBonus;

    public SpatialFunction()
    {
        Layers = new List<SpatialLayer>();
        LastCellBonus = 0f;
    }
}