using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class StageCombatController
{
    // Reconcile the existing UI tree rather than destroy/recreate the whole HUD.
    readonly Dictionary<(Transform,string,int),GameObject> uiNodes=new Dictionary<(Transform,string,int),GameObject>();
    readonly Dictionary<(Transform,string),int> uiCounts=new Dictionary<(Transform,string),int>();
    readonly HashSet<GameObject> uiUsed=new HashSet<GameObject>();
    void RegisterSavedUI()
    {
        uiNodes.Clear();
        foreach(var node in canvas.GetComponentsInChildren<CombatUINode>(true))
        {node.CaptureEdits();uiNodes[(node.transform.parent,node.key,node.occurrence)]=node.gameObject;}
    }
    GameObject UINode(Transform parent,string name,params Type[] components)
    {
        var baseKey=(parent,name);uiCounts.TryGetValue(baseKey,out int count);uiCounts[baseKey]=count+1;
        var key=(parent,name,count);
        if(!uiNodes.TryGetValue(key,out var go)||go==null)
        {go=new GameObject(name,components);go.transform.SetParent(parent,false);var node=go.AddComponent<CombatUINode>();node.key=name;node.occurrence=count;uiNodes[key]=go;}
        go.SetActive(true);go.transform.SetAsLastSibling();go.transform.localScale=Vector3.one;uiUsed.Add(go);return go;
    }
    static T Ensure<T>(GameObject go) where T:Component
    {var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
    void FinishUIUpdate()
    {foreach(var pair in uiNodes)if(pair.Value!=null){if(!uiUsed.Contains(pair.Value))pair.Value.SetActive(false);else pair.Value.GetComponent<CombatUINode>().ApplyEdits();}}
}
