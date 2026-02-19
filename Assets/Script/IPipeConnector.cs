using UnityEngine;

public interface IPipeConnector
{
    float snapRange { get; }
    Transform[] GetAllPoints();
}