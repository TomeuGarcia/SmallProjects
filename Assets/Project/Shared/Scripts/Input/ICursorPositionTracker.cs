using UnityEngine;

namespace Project.Shared.Scripts.Input
{
    public interface ICursorPositionTracker
    {
        Vector3 GetWorldPosition();
    }
}