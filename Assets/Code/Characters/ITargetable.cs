using UnityEngine;

public interface ITargetable
{
    Transform TargetTransform { get; }
    bool IsTargetable { get; }
}
