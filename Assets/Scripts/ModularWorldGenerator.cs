using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class ModularWorldGenerator : MonoBehaviour
{
	public Module[] _Modules;
    public Module _StartModule;
    public Module _BossModule;
    public Module _CloseModule;
    public string[] _SmoothModules;
    public int _Iterations = 7;
    public int _SmoothIterations = 5;
    private GameObject MainGameObject;

    public struct DataStruct
    {
        public DataStruct(Bounds boundsValue, string strValue)
        {
            boundsData = boundsValue;
            StringData = strValue;
        }

        public Bounds boundsData { get; private set; }
        public string StringData { get; private set; }
    }
    public List<DataStruct> DataBoundsList = new List<DataStruct>();

    void Awake()
	{
        MainGameObject = new GameObject();
        DataBoundsList = new List<DataStruct>();
    }

    private void Update()
    {
        //System.Threading.Thread.Sleep(5000);
        if (Input.GetMouseButtonDown(0))
        {
            StartCreate();
        }
        if (Input.GetMouseButtonDown(1))
        {
            DestroyOld();
        }
    }

    private void DestroyOld()
    {
        Destroy(MainGameObject);
        MainGameObject = new GameObject();
    }

    private void StartCreate()
    {
        DataBoundsList = new List<DataStruct>();
        var startModule = (Module)Instantiate(_StartModule, transform.position, transform.rotation, MainGameObject.transform);
        AddBound(startModule.GetComponent<BoxCollider>().bounds,startModule.name);
        var pendingExits = new List<ModuleConnector>(startModule.GetExits());
        List<ModuleConnector> globalExits = new List<ModuleConnector>(pendingExits);

        int i = 0;
        for (int iteration = 0; iteration < _Iterations; iteration++)
        {
            Debug.Log("here1");
            var newExits = new List<ModuleConnector>();
            foreach (var pendingExit in pendingExits)
            {
                Debug.Log("here2");
                //ModuleConnector lastconnector = new ModuleConnector();
                bool found = false;
                List<string> pendingExitList = pendingExit.Tags.ToList();
                while (pendingExitList.Count > 0 && !found)
                {
                    found = false;
                    string newTag = GetRandom(pendingExitList);
                    List<Module> modulesForTag = _Modules.Where(m => m.Tags.Contains(newTag)).ToList();
                    while (modulesForTag.Count > 0 && !found)
                    {
                        Module newModulePref = GetRandom(modulesForTag);
                        Module newModule = (Module)Instantiate(newModulePref, MainGameObject.transform);
                        newModule.name = newModule.name + "_" + ++i;
                        List<ModuleConnector> connectorsInModule = newModule.GetExits().ToList();
                        while (connectorsInModule.Count > 0 && !found)
                        {
                            ModuleConnector connector = connectorsInModule.FirstOrDefault(x => x.IsDefault) ?? GetRandom(connectorsInModule);
                            MatchExits(pendingExit, connector);
                            if (!checkBoundData(newModule.GetComponent<BoxCollider>().bounds, newModule.name))
                            {
                                Debug.Log("intersect");
                                //if setting fails remove block from random list. TODO :)
                                connectorsInModule.Remove(connector);
                                //when nothing found add to globalexits for block and smoothing
                                //lastconnector = connector;
                            }
                            else
                            {
                                connector.ConnectedConnector = pendingExit;
                                pendingExit.ConnectedConnector = connector;
                                //remove  pendingExit from global one if fits
                                globalExits.Remove(pendingExit);
                                newExits.AddRange(newModule.GetExits().ToList().Where(e => e != connector));

                                found = true;
                            }
                        }
                        if (!found) {
                            Destroy(newModule.gameObject);
                            modulesForTag.Remove(newModulePref);
                        }
                    }
                    if (!found)
                        pendingExitList.Remove(newTag);
                }
            }    
            globalExits.AddRange(newExits);
            pendingExits = newExits;
        }


        var BossModule = (Module)Instantiate(_BossModule, MainGameObject.transform);
        string BossModuleName = BossModule.name;
        var BossModuleExits = BossModule.GetExits();
        var BossexitToMatch = BossModuleExits.FirstOrDefault(x => x.IsDefault) ?? GetRandom(BossModuleExits);
        bool BossRoomSet = false;
        List<string> SmoothList = new List<string>(_SmoothModules.ToList());
        bool removableLeft = true;
        while (removableLeft)
        {
            removableLeft = false;
            for (int j = 0; j < globalExits.Count; j++)
            {
                ModuleConnector tryExit = globalExits[j];
                i++;
                //Set BossRoom
                if (!BossRoomSet)
                {
                    BossModule.name = BossModuleName + "_" + i;
                    MatchExits(tryExit, BossexitToMatch);
                    if (checkBoundData(BossModule.GetComponent<BoxCollider>().bounds, BossModule.name))
                    {
                        //RemoveGlobalExits.Add(tryExit);
                        BossexitToMatch.ConnectedConnector = tryExit;
                        tryExit.ConnectedConnector = BossexitToMatch;
                        BossRoomSet = true;
                        globalExits.Remove(tryExit);
                        removableLeft = true;
                        break;
                    }
                }

                //Smooth Blocks
                GameObject ParentGo = tryExit.transform.parent.gameObject; //for destroy
                Module ParentModule = ParentGo.GetComponent<Module>();
                List<string> ParentModuleTags = ParentModule.Tags.ToList();
                if (ParentModuleTags.Except(SmoothList).ToList().Count <= 0)
                {
                    int connectedConnectors = 0;
                    ModuleConnector newExit = null;
                    List<ModuleConnector> connectors = new List<ModuleConnector>(ParentModule.GetExits().ToList());
                    foreach (ModuleConnector mc in connectors)
                    {
                        if (mc != tryExit && mc.ConnectedConnector != null)
                        {
                            connectedConnectors++;
                            newExit = mc.ConnectedConnector;
                        }
                    }
                    if (connectedConnectors == 1)
                    {
                        removableLeft = true;
                        newExit.ConnectedConnector = null;
                        globalExits.RemoveAll(e => connectors.Contains(e));
                        globalExits.Add(newExit);
                        Destroy(ParentGo);
                        break;
                    }
                }
            }
        }

        //Set Close Blocks
        foreach (ModuleConnector tryExit in globalExits)
        {
            SetCloseBlock(tryExit, i++);
        }
    }

    private void SetCloseBlock(ModuleConnector CloseConnector,int numberForName)
    {
        var CloseModule1 = (Module)Instantiate(_CloseModule, MainGameObject.transform);
        CloseModule1.name = CloseModule1.name + "_" + numberForName;
        var CloseModule1Exits = CloseModule1.GetExits();
        var Closeexit1ToMatch = CloseModule1Exits.FirstOrDefault(x => x.IsDefault) ?? GetRandom(CloseModule1Exits);
        MatchExits(CloseConnector, Closeexit1ToMatch);
    }

    private void MatchExits(ModuleConnector oldExit, ModuleConnector newExit)
	{
		var newModule = newExit.transform.parent;
		var forwardVectorToMatch = -oldExit.transform.forward;
		var correctiveRotation = Azimuth(forwardVectorToMatch) - Azimuth(newExit.transform.forward);
		newModule.RotateAround(newExit.transform.position, Vector3.up, correctiveRotation);
		var correctiveTranslation = oldExit.transform.position - newExit.transform.position;
		newModule.transform.position += correctiveTranslation;
	}

    private static float Azimuth(Vector3 vector)
    {
        return Vector3.Angle(Vector3.forward, vector) * Mathf.Sign(vector.x);
    }

    private static TItem GetRandom<TItem>(TItem[] array)
	{
		return array[Random.Range(0, array.Length)];
	}

    private static TItem GetRandom<TItem>(List<TItem> list)
    {
        return list[Random.Range(0, list.Count<TItem>())];
    }

	private static Module GetRandomWithTag(IEnumerable<Module> modules, string tagToMatch)
	{
		var matchingModules = modules.Where(m => m.Tags.Contains(tagToMatch)).ToArray();
        return GetRandom(matchingModules);
	}

    public void AddBound(Bounds bounds, string modulename)
    {
        DataBoundsList.Add(new DataStruct(bounds, modulename));
    }

    public bool checkBoundData(Bounds nowbounds, string nowname)
    {
        foreach (DataStruct data in DataBoundsList)
        {
            if (nowbounds.Intersects(data.boundsData))
            {
                //Debug.Log("Intersects: " + nowname + " collides with " + data.StringData);
                return false;
            }
        }
        this.AddBound(nowbounds, nowname);
        return true;
    }
}
