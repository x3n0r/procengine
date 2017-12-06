using UnityEngine;
using System.Collections.Generic;

public class Module : MonoBehaviour
{
	public string[] Tags;

    public ModuleConnector[] GetExits()
	{
        return GetComponentsInChildren<ModuleConnector>();
	}
}
