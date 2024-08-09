using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LDS2;
using UnityEngine.Rendering;
using System.Linq;
using System;

public class NetGameMaster : MonoBehaviour
{
    [Header("Network Settings")]
    public int UpdatePerSec = 10;
    public GameObject placeholderObj;
    public int timeout = 10;

    [Header("Current")]
    public float Networkdelta = 1;
    public GameObject playerObj;
    RelayPrototype relay;
    public ushort playerId; // This will be used to identify who sent it, 0 is always Host
    bool playerIdReq = false;
    public float playerIdReqResendTimer = 3;
    float playerIdReqResend = 3;
    public bool isNetwork; // Is Network Running?
    public bool isHost; // Is Host?
    public List<WatchObject> watchObjects = new List<WatchObject>(); //Objects that are being watched

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (playerObj == null) { playerObj = GameObject.FindGameObjectWithTag("Player"); }
        if (relay == null) { relay = GameObject.Find("RelayPrototype").GetComponent<RelayPrototype>(); }
        if (isNetwork) { 
            if (Networkdelta < 0)
            {
                Networkdelta += 1f / UpdatePerSec;
                NetworkUpdate();
            }
            else
            {
                Networkdelta -= Time.deltaTime;
            }
        }

        isNetwork = relay.isNetwork;
        isHost = relay.isHost;
        if (!isNetwork)
        {
            if (watchObjects.Count == 0) { return; }
            foreach (WatchObject watchObject in watchObjects)
            {
                GameObject.Destroy(watchObject.gameObject);
            }
            watchObjects.Clear();
            playerId = 0;
            playerIdReq = false;
        }

        //Advance Timeout on watchobjects with objectid 0
        foreach (WatchObject watchObject in watchObjects)
        {
            if (watchObject.objectId == 0)
            {
                watchObject.timeout -= Time.deltaTime;
                if (watchObject.timeout < 0)
                {
                    GameObject.Destroy(watchObject.gameObject);
                    watchObjects.Remove(watchObject);
                }
            }
        }

        //Request PlayerId
        if (playerId == 0)
        {
            if (playerIdReqResend < 0) 
            {
                playerIdReqResend += playerIdReqResendTimer;
                playerIdReq = false;
            }
            playerIdReqResend -= Time.deltaTime;
        }
    }

    void NetworkUpdate()
    {
        //Create LData
        LDataSerializerE ldata = new LDataSerializerE();

        //Used Identifiers
        //0x99 - All Clients And Host Give Gameplay Player Data (To Everyone)
        //0x98 - Client Get Id (Only To Host)
        //0x97 - Feedback for Client Get Id (Only To Client Feedback)
        //0x96 - (Only To Host Commands)

        #region Send Data

        //Host Only
        if (isHost)
        {
            //Get PlayerId to 1 for Hosts (0 means empty)
            playerId = 1;
        }

        //Client Only
        if (!isHost && !playerIdReq)
        {
            playerIdReq = true;
            //Get PlayerId for Clients (0 means empty)

            //Send Byte for identifier (Byte)
            LDataSerializerE cldata = new LDataSerializerE();
            cldata.Add((byte)0x98); // This is the code for contacting Relay
            relay.RpcSend(cldata.GetBytes());
            Debug.Log("Player Id Requested");
        }

        //All
        if (playerId != 0)
        {
            //Send Player Current Status
            Vector3 playerpos = playerObj.transform.position;
            //Send Byte for identifier (Byte)
            ldata.Add((byte)0x99);
            //Send PlayerId (Short)
            ldata.Add(playerId);
            //Send ObjectId (0 for PlayerObject) (Short)
            ldata.Add((ushort)0);
            //Send Float Position XYZ (Float)(Float)(Float)
            ldata.Add(playerpos.x);
            ldata.Add(playerpos.y);
            ldata.Add(playerpos.z);
            //Sent Data = (Byte)(Short)(Short)(Float)(Float)(Float)
        }
        #endregion

        #region Receive Data
        //Receive Data
        while (true)
        {
            byte[] data = relay.GetReceivedData();
            if (data == null) { break; }
            Debug.Log("reading received data");
            ldata = new LDataSerializerE(data);
            while (ldata.position != data.Length) 
            { 
                //Identifier
                byte fbyte = ldata.GetByteNext();
                Debug.Log("Identifier: " + fbyte.ToString());
                if (fbyte == (byte)0x99)
                {
                    //PlayerId
                    ushort cplayerId = ldata.GetUShortNext();
                    if (cplayerId == playerId) { return; }
                    ushort cobjectId = ldata.GetUShortNext();
                    Vector3 cpos = new Vector3();
                    cpos.x = ldata.GetFloatNext();
                    cpos.y = ldata.GetFloatNext();
                    cpos.z = ldata.GetFloatNext();
                    HandleWatchObject(cplayerId, cobjectId, cpos);
                }
                if (fbyte == (byte)0x97)
                {
                    playerId = ldata.GetUShortNext();
                    Debug.Log("PlayerId Received: " + playerId.ToString());
                }
            }
        }
        #endregion

        if (isHost) { relay.GetComponent<RelayPrototype>().CmdSend(ldata.GetBytes()); }
        else { relay.GetComponent<RelayPrototype>().RpcSend(ldata.GetBytes()); }
    }

    void HandleWatchObject(ushort cplayerId, ushort cobjectId, Vector3 cpos)
    {
        Debug.Log("(playerId:objectId): " + cplayerId.ToString() + ":" + cobjectId.ToString());
        Debug.Log(cpos);
        bool isExists = false;
        WatchObject watchObject = new WatchObject();
        foreach (WatchObject getWatchObject in watchObjects)
        {
            if (cplayerId == getWatchObject.playerId && cobjectId == getWatchObject.objectId)
            {
                isExists = true;
                watchObject = getWatchObject;
                break;
            }
        }
        if (!isExists)
        {
            WatchObject newWatchObject = new WatchObject();
            newWatchObject.playerId = cplayerId;
            newWatchObject.objectId = cobjectId;
            newWatchObject.position = cpos;
            newWatchObject.gameObject = Instantiate(placeholderObj);
            watchObjects.Add(newWatchObject);
            watchObject = newWatchObject;
            Debug.Log("watchObject created successfully: (" + cplayerId + ":" + cobjectId+ ")");
        }
        if (watchObject.objectId == 0)
        {
            watchObject.timeout = timeout;
        }
        watchObject.position = cpos;
        watchObject.gameObject.transform.position = watchObject.position;
    }
}