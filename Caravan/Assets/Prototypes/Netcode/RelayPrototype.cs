using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Windows.Input;
using LDS2;

/// <summary>
/// A simple sample showing how to use the Relay Allocation package with the Unity Transport Protocol (UTP).
/// It demonstrates how UTP can be used as either Hosts or Joining Players, covering the entire connection flow.
/// As a bonus, a simple demonstration of Relaying messages from Host to Players, and vice versa, is included.
/// </summary>
public class RelayPrototype : MonoBehaviour
{
    #region
    // GUI GameObjects

    public Button ButtonClientAsHost;
    public Button ButtonClientAsPlayer;

    /// <summary>
    /// The textbox displaying the Host's Player Id.
    /// </summary>
    public Text HostPlayerIdText;

    /// <summary>
    /// The textbox displaying the Player's Player Id.
    /// </summary>
    public Text PlayerPlayerIdText;

    /// <summary>
    /// The dropdown displaying the region.
    /// </summary>
    public Dropdown RegionsDropdown;

    /// <summary>
    /// The textbox displaying the Allocation Id.
    /// </summary>
    public Text HostAllocationIdText;

    /// <summary>
    /// The textbox displaying the Join Code.
    /// </summary>
    public InputField JoinCodeGetInput;

    /// <summary>
    /// The input field for the Join Code that the Player inputs to join the Host's Relay server.
    /// </summary>
    public InputField JoinCodeInput;

    /// <summary>
    /// The textbox displaying the Allocation Id of the joined allocation.
    /// </summary>
    public Text PlayerAllocationIdText;

    /// <summary>
    /// The textbox displaying the Allocation Id of the joined allocation.
    /// </summary>
    public Text PlayerRegionText;

    /// <summary>
    /// The textbox displaying whether or not the Host is bound.
    /// </summary>
    public Text HostBoundText;

    /// <summary>
    /// The textbox displaying whether or not the Player is bound.
    /// </summary>
    public Text PlayerBoundText;

    /// <summary>
    /// The textbox displaying whether or not the Player is connected to the Host.
    /// </summary>
    public Text PlayerConnectedText;

    /// <summary>
    /// The input field for the message to send from the Host to the Player.
    /// </summary>
    public InputField HostMessageInput;

    /// <summary>
    /// The input field for the message to send from the Player to the Host.
    /// </summary>
    public InputField PlayerMessageInput;

    /// <summary>
    /// Reference to the MainMenuPanel game object.
    /// </summary>
    public GameObject MainMenuPanel;

    /// <summary>
    /// Reference to the HostPanel game object.
    /// </summary>
    public GameObject HostPanel;

    /// <summary>
    /// Reference to the PlayerPanel game object.
    /// </summary>
    public GameObject PlayerPanel;

    /// <summary>
    /// The textbox displaying the latest message received by the Host.
    /// </summary>
    public Text HostMessageReceivedText;

    /// <summary>
    /// The textbox displaying the number of the Host's connected players.
    /// </summary>
    public Text HostConnectedPlayersText;

    /// <summary>
    /// The textbox displaying the latest message received by the Player.
    /// </summary>
    public Text PlayerMessageReceivedText;
    #endregion

    // GUI vars
    string joinCode = "n/a";
    string playerId = "Not signed in";
    string autoSelectRegionName = "auto-select (QoS)";
    int regionAutoSelectIndex = 0;
    List<Region> regions = new List<Region>();
    List<string> regionOptions = new List<string>();
    public bool isNetwork;
    //string playerLatestMessageReceived;

    // Allocation response objects
    Allocation hostAllocation;
    JoinAllocation playerAllocation;

    // Control vars
    public bool isHost;
    bool isPlayer;
    int reservedPlayerId = 2;

    // UTP vars
    NetworkDriver hostDriver;
    NetworkDriver playerDriver;
    NativeList<NetworkConnection> serverConnections;
    NetworkConnection clientConnection;

    // Stored Data Bytes
    byte[][] receivedDatas = new byte[64][];
    List<ReservedDataMessage> reserveddatamessage = new List<ReservedDataMessage>();

    async void Start()
    {
        // Set GUI to Main Menu
        MainMenuPanel.SetActive(true);
        HostPanel.SetActive(false);
        PlayerPanel.SetActive(false);

        // Allow Enter key to be used for input fields
        //AddEnterKeyListenerToInputField(HostMessageInput, OnHostSendMessage);
        //AddEnterKeyListenerToInputField(PlayerMessageInput, OnPlayerSendMessage);
        AddEnterKeyListenerToInputField(JoinCodeInput, OnJoin);

        // Initialize Unity Services
        await UnityServices.InitializeAsync();
    }

    void AddEnterKeyListenerToInputField(InputField inputField, Action clickButton)
    {
        inputField.onEndEdit.AddListener(_ =>
        {
            // Submit == Enter key
            if (Input.GetButton("Submit"))
            {
                clickButton();
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
            }
        });
    }

    void Update()
    {
        if (isHost)
        {
            UpdateHost();
            UpdateHostUI();
        }
        else if (isPlayer)
        {
            UpdatePlayer();
            UpdatePlayerUI();
        }
        if (UnityServices.State.ToString() == "Initialized" && playerId == "Not signed in")
        {
            playerId = "Signing In...";
            OnSignIn();
        }
    }

    void OnDestroy()
    {
        // Cleanup objects upon exit
        if (isHost)
        {
            hostDriver.Dispose();
            serverConnections.Dispose();
        }
        else if (isPlayer)
        {
            playerDriver.Dispose();
        }
    }

    void UpdateHostUI()
    {
        HostPlayerIdText.text = playerId;
        RegionsDropdown.interactable = regions.Count > 0;
        RegionsDropdown.options?.Clear();
        RegionsDropdown.AddOptions(new List<string> {autoSelectRegionName});  // index 0 is always auto-select (use QoS)
        RegionsDropdown.AddOptions(regionOptions);
        if (!String.IsNullOrEmpty(hostAllocation?.Region))
        {
            if (regionOptions.Count == 0)
            {
                RegionsDropdown.AddOptions(new List<String>(new[] { hostAllocation.Region }));
            }
            RegionsDropdown.value = RegionsDropdown.options.FindIndex(option => option.text == hostAllocation.Region);
        }
        //HostAllocationIdText.text = hostAllocation?.AllocationId.ToString();
        JoinCodeGetInput.text = joinCode;
        HostBoundText.text = hostDriver.IsCreated ? hostDriver.Bound.ToString() : false.ToString();
        //HostConnectedPlayersText.text = serverConnections.IsCreated ? serverConnections.Length.ToString() : 0.ToString();
        //HostMessageReceivedText.text = hostLatestMessageReceived;
    }

    void UpdatePlayerUI()
    {
        PlayerPlayerIdText.text = playerId;
        //PlayerAllocationIdText.text = playerAllocation?.AllocationId.ToString();
        PlayerRegionText.text = playerAllocation?.Region;
        //PlayerBoundText.text = playerDriver.IsCreated ? playerDriver.Bound.ToString() : false.ToString();
        PlayerConnectedText.text = clientConnection.IsCreated.ToString();
        //PlayerMessageReceivedText.text = playerLatestMessageReceived;
    }

    /// <summary>
    /// Event handler for when the Start game as Host client button is clicked.
    /// </summary>
    public void OnStartClientAsHost()
    {
        MainMenuPanel.SetActive(false);
        HostPanel.SetActive(true);
        isHost = true;

        OnRegion();
    }

    /// <summary>
    /// Event handler for when the Start game as Player client button is clicked.
    /// </summary>
    public void OnStartClientAsPlayer()
    {
        MainMenuPanel.SetActive(false);
        PlayerPanel.SetActive(true);
        isPlayer = true;

        
    }

    /// <summary>
    /// Event handler for when the Sign In button is clicked.
    /// </summary>
    public async void OnSignIn()
    {
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        playerId = AuthenticationService.Instance.PlayerId;

        Debug.Log($"Signed in. Player ID: {playerId}");
        ButtonClientAsHost.interactable = true;
        ButtonClientAsPlayer.interactable = true;
    }

    /// <summary>
    /// Event handler for when the Get Regions button is clicked.
    /// </summary>
    public async void OnRegion()
    {
        Debug.Log("Host - Getting regions.");
        var allRegions = await RelayService.Instance.ListRegionsAsync();
        regions.Clear();
        regionOptions.Clear();
        foreach (var region in allRegions)
        {
            Debug.Log(region.Id + ": " + region.Description);
            regionOptions.Add(region.Id);
            regions.Add(region);
        }
    }

    string GetRegionOrQosDefault()
    {
        // Return null (indicating to auto-select the region/QoS) if regions list is empty OR auto-select/QoS is chosen
        if (!regions.Any() || RegionsDropdown.value == regionAutoSelectIndex)
        {
            return null;
        }
        // else use chosen region (offset -1 in dropdown due to first option being auto-select/QoS)
        return regions[RegionsDropdown.value - 1].Id;
    }

    public void OnHost()
    {
        OnAllocate();
    }

    /// <summary>
    /// Event handler for when the Allocate button is clicked.
    /// </summary>
    public async void OnAllocate()
    {
        Debug.Log("Host - Creating an allocation. Upon success, I have 10 seconds to BIND to the Relay server that I've allocated.");

        // Determine region to use (user-selected or auto-select/QoS)
        string region = GetRegionOrQosDefault();
        Debug.Log($"The chosen region is: {region ?? autoSelectRegionName}");

        // Set max connections. Can be up to 100, but note the more players connected, the higher the bandwidth/latency impact.
        int maxConnections = 4;

        // Important: Once the allocation is created, you have ten seconds to BIND, else the allocation times out.
        hostAllocation = await RelayService.Instance.CreateAllocationAsync(maxConnections, region);
        Debug.Log($"Host Allocation ID: {hostAllocation.AllocationId}, region: {hostAllocation.Region}");

        // Initialize NetworkConnection list for the server (Host).
        // This list object manages the NetworkConnections which represent connected players.
        serverConnections = new NativeList<NetworkConnection>(maxConnections, Allocator.Persistent);

        // Bind Host
        OnBindHost();
    }

    /// <summary>
    /// Event handler for when the Bind Host to Relay (UTP) button is clicked.
    /// </summary>
    public void OnBindHost()
    {
        Debug.Log("Host - Binding to the Relay server using UTP.");

        // Extract the Relay server data from the Allocation response.
        var relayServerData = new RelayServerData(hostAllocation, "udp");

        // Create NetworkSettings using the Relay server data.
        var settings = new NetworkSettings();
        settings.WithRelayParameters(ref relayServerData);

        // Create the Host's NetworkDriver from the NetworkSettings.
        hostDriver = NetworkDriver.Create(settings);

        // Bind to the Relay server.
        if (hostDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
        {
            Debug.LogError("Host client failed to bind");
        }
        else
        {
            if (hostDriver.Listen() != 0)
            {
                Debug.LogError("Host client failed to listen");
            }
            else
            {
                Debug.Log("Host client bound to Relay server");
            }
        }

        //Get Join Code
        OnJoinCode();
    }

    /// <summary>
    /// Event handler for when the Get Join Code button is clicked.
    /// </summary>
    public async void OnJoinCode()
    {
        Debug.Log("Host - Getting a join code for my allocation. I would share that join code with the other players so they can join my session.");

        try
        {
            joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);
            Debug.Log("Host - Got join code: " + joinCode);
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex.Message + "\n" + ex.StackTrace);
        }
    }

    public void OnCopyJoinCode()
    {
        ClipboardHelper.CopyToClipboard(joinCode);
    }

    /// <summary>
    /// Event handler for when the Join button is clicked.
    /// </summary>
    public async void OnJoin()
    {
        // Input join code in the respective input field first.
        if (String.IsNullOrEmpty(JoinCodeInput.text))
        {
            Debug.LogError("Please input a join code.");
            return;
        }

        Debug.Log("Player - Joining host allocation using join code. Upon success, I have 10 seconds to BIND to the Relay server that I've allocated.");

        try
        {
            playerAllocation = await RelayService.Instance.JoinAllocationAsync(JoinCodeInput.text);
            Debug.Log("Player Allocation ID: " + playerAllocation.AllocationId);
            OnBindPlayer();
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex.Message + "\n" + ex.StackTrace);
        }
    }

    /// <summary>
    /// Event handler for when the Bind Player to Relay (UTP) button is clicked.
    /// </summary>
    public void OnBindPlayer()
    {
        Debug.Log("Player - Binding to the Relay server using UTP.");

        // Extract the Relay server data from the Join Allocation response.
        var relayServerData = new RelayServerData(playerAllocation, "udp");

        // Create NetworkSettings using the Relay server data.
        var settings = new NetworkSettings();
        settings.WithRelayParameters(ref relayServerData);

        // Create the Player's NetworkDriver from the NetworkSettings object.
        playerDriver = NetworkDriver.Create(settings);

        // Bind to the Relay server.
        if (playerDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
        {
            Debug.LogError("Player client failed to bind");
        }
        else
        {
            Debug.Log("Player client bound to Relay server");
            OnConnectPlayer();
        }
    }

    /// <summary>
    /// Event handler for when the Connect Player to Relay (UTP) button is clicked.
    /// </summary>
    public void OnConnectPlayer()
    {
        Debug.Log("Player - Connecting to Host's client.");

        // Sends a connection request to the Host Player.
        clientConnection = playerDriver.Connect();
    }

    /// <summary>
    /// Event handler for when the Send message from Host to Relay (UTP) button is clicked.
    /// </summary>
    public void OnHostSendMessage()
    {
        if (serverConnections.Length == 0)
        {
            Debug.LogError("No players connected to send messages to.");
            return;
        }

        // Get message from the input field, or default to the placeholder text.
        var msg = !String.IsNullOrEmpty(HostMessageInput.text) ? HostMessageInput.text : HostMessageInput.placeholder.GetComponent<Text>().text;

        // In this sample, we will simply broadcast a message to all connected clients.
        for (int i = 0; i < serverConnections.Length; i++)
        {
            if (hostDriver.BeginSend(serverConnections[i], out var writer) == 0)
            {
                // Send the message. Aside from FixedString32, many different types can be used.
                writer.WriteFixedString32(msg);
                hostDriver.EndSend(writer);
            }
        }
    }

    /// <summary>
    /// Event handler for when the Send message from Player to Host (UTP) button is clicked.
    /// </summary>
    public void OnPlayerSendMessage()
    {
        if (!clientConnection.IsCreated)
        {
            Debug.LogError("Player is not connected. No Host client to send message to.");
            return;
        }

        // Get message from the input field, or default to the placeholder text.
        var msg = !String.IsNullOrEmpty(PlayerMessageInput.text) ? PlayerMessageInput.text : PlayerMessageInput.placeholder.GetComponent<Text>().text;
        if (playerDriver.BeginSend(clientConnection, out var writer) == 0)
        {
            // Send the message. Aside from FixedString32, many different types can be used.
            writer.WriteFixedString32(msg);
            playerDriver.EndSend(writer);
        }
    }

    /// <summary>
    /// Event handler for when the DisconnectPlayers (UTP) button is clicked.
    /// </summary>
    public void OnDisconnectPlayers()
    {
        if (serverConnections.Length == 0)
        {
            Debug.LogError("No players connected to disconnect.");
            return;
        }

        // In this sample, we will simply disconnect all connected clients.
        for (int i = 0; i < serverConnections.Length; i++)
        {
            // This sends a disconnect event to the destination client,
            // letting them know they are disconnected from the Host.
            hostDriver.Disconnect(serverConnections[i]);

            // Here, we set the destination client's NetworkConnection to the default value.
            // It will be recognized in the Host's Update loop as a stale connection, and be removed.
            serverConnections[i] = default(NetworkConnection);
        }
    }

    /// <summary>
    /// Event handler for when the Disconnect (UTP) button is clicked.
    /// </summary>
    public void OnDisconnect()
    {
        // This sends a disconnect event to the Host client,
        // letting them know they are disconnecting.
        playerDriver.Disconnect(clientConnection);

        // We remove the reference to the current connection by overriding it.
        clientConnection = default(NetworkConnection);
    }

    //Send databytes to all Clients
    public void CmdSend(byte[] data)
    {
        // In this sample, we will simply broadcast a message to all connected clients.
        for (int i = 0; i < serverConnections.Length; i++)
        {
            LDataSerializerE eldata = new LDataSerializerE();
            List<ReservedDataMessage> removeReserved = new List<ReservedDataMessage>();
            foreach (ReservedDataMessage rdm in reserveddatamessage)
            {
                eldata.Add(rdm.data);
                removeReserved.Add(rdm);
            }
            byte[] extradata = eldata.GetBytesRaw();
            foreach (ReservedDataMessage rdm in removeReserved)
            {
                reserveddatamessage.Remove(rdm);
            }
            if (hostDriver.BeginSend(serverConnections[i], out var writer) == 0)
            {
                byte[] senddata = data;
                // Add Extra Message if Identified
                foreach (ReservedDataMessage rdm in reserveddatamessage)
                {
                    if (rdm.connInternalId == serverConnections[i].InternalId)
                    {
                        LDataSerializerE ldata = new LDataSerializerE(senddata);
                        ldata.EditData();
                        ldata.Add(rdm.data);
                        senddata = ldata.GetBytes();
                        Debug.Log("Extra Data Sent");
                        reserveddatamessage.Remove(rdm);
                    }
                }
                eldata = new LDataSerializerE(senddata);
                eldata.EditData();
                eldata.Add(extradata);
                senddata = eldata.GetBytes();
                // Send the message
                writer.WriteBytes(ByteArrayConverter.ConvertToNativeArray(senddata, Allocator.Temp));
                hostDriver.EndSend(writer);
            }
        }
    }

    //Send databytes to server
    public void RpcSend(byte[] data)
    {
        if (!clientConnection.IsCreated)
        {
            Debug.LogError("Player is not connected. No Host client to send message to.");
            return;
        }
        if (playerDriver.BeginSend(clientConnection, out var writer) == 0)
        {
            // Send the message
            writer.WriteBytes(ByteArrayConverter.ConvertToNativeArray(data, Allocator.Temp));
            playerDriver.EndSend(writer);
        }
    }

    void UpdateHost()
    {
        // Skip update logic if the Host is not yet bound.
        if (!hostDriver.IsCreated || !hostDriver.Bound)
        {
            isNetwork = false;
            return;
        }

        // Tells that the Session is currently active
        isNetwork = true;

        // This keeps the binding to the Relay server alive,
        // preventing it from timing out due to inactivity.
        hostDriver.ScheduleUpdate().Complete();

        // Clean up stale connections.
        for (int i = 0; i < serverConnections.Length; i++)
        {
            if (!serverConnections[i].IsCreated)
            {
                Debug.Log("Stale connection removed");
                serverConnections.RemoveAt(i);
                --i;
            }
        }

        // Accept incoming client connections.
        NetworkConnection incomingConnection;
        while ((incomingConnection = hostDriver.Accept()) != default(NetworkConnection))
        {
            // Adds the requesting Player to the serverConnections list.
            // This also sends a Connect event back the requesting Player,
            // as a means of acknowledging acceptance.
            Debug.Log("Accepted an incoming connection.");
            serverConnections.Add(incomingConnection);
        }

        // Process events from all connections.
        for (int i = 0; i < serverConnections.Length; i++)
        {
            Assert.IsTrue(serverConnections[i].IsCreated);
 
            // Resolve event queue.
            NetworkEvent.Type eventType;
            while ((eventType = hostDriver.PopEventForConnection(serverConnections[i], out var stream)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    // Handle Relay events.
                    case NetworkEvent.Type.Data:
                        Debug.Log("Received Client data");
                        // Example: Read the size of the incoming data first
                        int dataSize = stream.ReadUShort(); // Assume the first 2 bytes represent the size
                        dataSize -= 1;

                        // Prepare a NativeArray to hold both the size and the data
                        NativeArray<byte> combinedData = new NativeArray<byte>(2 + dataSize, Allocator.Temp);

                        // Copy the dataSize into the first 2 bytes of the array
                        byte[] sizeBytes = System.BitConverter.GetBytes(dataSize);
                        for (int j = 0; j < 2; j++)
                        {
                            combinedData[j] = sizeBytes[j];
                        }

                        // Create a NativeArray to hold the data
                        //NativeArray<byte> data = new NativeArray<byte>(dataSize, Allocator.Temp);

                        // Read the data into the NativeArray
                        stream.ReadBytes(combinedData.GetSubArray(2, dataSize));

                        // Check if there is a specific byte code that would need relay specific functions
                        LDataSerializerE ldata = new LDataSerializerE(ByteArrayConverter.ConvertToByteArray(combinedData));
                        byte fbyte = ldata.GetByteNext();
                        if (fbyte == (byte)0x98)
                        {
                            ushort givePlayerId = (ushort)reservedPlayerId;
                            reservedPlayerId += 1;
                            // Reserve the playerId to the message
                            ReservedDataMessage rdm = new ReservedDataMessage();
                            rdm.connInternalId = serverConnections[i].InternalId;
                            LDataSerializerE ndata = new LDataSerializerE();
                            ndata.Add((byte)0x97);
                            ndata.Add(givePlayerId);
                            rdm.data = ndata.GetBytesRaw();
                            reserveddatamessage.Add(rdm);
                            Debug.Log("Server received special data");
                            break;
                        }

                        // Put the data into the receivedDatas
                        AppendReceivedData(ByteArrayConverter.ConvertToByteArray(combinedData));

                        Debug.Log("Server received data");

                        // Reserve the client data to the message
                        ReservedDataMessage rdm2 = new ReservedDataMessage();
                        rdm2.connInternalId = -1;
                        LDataSerializerE ndata2 = new LDataSerializerE();
                        rdm2.data = ndata2.GetBytes();
                        reserveddatamessage.Add(rdm2);
                        Debug.Log("Data is not limited to Server only, Sending to all other clients");
                        break;

                        //FixedString32Bytes msg = stream.ReadFixedString32();
                        //Debug.Log($"Server received msg: {msg}");
                        ////hostLatestMessageReceived = msg.ToString();
                        //break;

                    // Handle Disconnect events.
                    case NetworkEvent.Type.Disconnect:
                        Debug.Log("Server received disconnect from client");
                        serverConnections[i] = default(NetworkConnection);
                        break;
                }
            }
        }
    }

    void UpdatePlayer()
    {
        // Skip update logic if the Player is not yet bound.
        if (!playerDriver.IsCreated || !playerDriver.Bound)
        {
            isNetwork = false;
            return;
        }

        // Tells that the Session is currently active
        isNetwork = true;

        // This keeps the binding to the Relay server alive,
        // preventing it from timing out due to inactivity.
        playerDriver.ScheduleUpdate().Complete();

        // Resolve event queue.
        NetworkEvent.Type eventType;
        while ((eventType = clientConnection.PopEvent(playerDriver, out var stream)) != NetworkEvent.Type.Empty)
        {
            switch (eventType)
            {
                // Handle Relay events.
                case NetworkEvent.Type.Data:
                    // Example: Read the size of the incoming data first
                    int dataSize = stream.ReadUShort(); // Assume the first 2 bytes represent the size
                    dataSize -= 1;

                    // Prepare a NativeArray to hold both the size and the data
                    NativeArray<byte> combinedData = new NativeArray<byte>(2 + dataSize, Allocator.Temp);

                    // Copy the dataSize into the first 2 bytes of the array
                    byte[] sizeBytes = System.BitConverter.GetBytes(dataSize);
                    for (int i = 0; i < 2; i++)
                    {
                        combinedData[i] = sizeBytes[i];
                    }

                    // Create a NativeArray to hold the data
                    //NativeArray<byte> data = new NativeArray<byte>(dataSize, Allocator.Temp);

                    // Read the data into the NativeArray
                    stream.ReadBytes(combinedData.GetSubArray(2, dataSize));

                    // Check if there is a specific byte code that would need relay specific functions
                    LDataSerializerE ldata = new LDataSerializerE(ByteArrayConverter.ConvertToByteArray(combinedData));
                    byte fbyte = ldata.GetByteNext();
                    if (fbyte == (byte)0x98)
                    {
                        break;
                    }

                    // Put the data into the receivedDatas
                    AppendReceivedData(ByteArrayConverter.ConvertToByteArray(combinedData));

                    Debug.Log("Player received data");
                    //playerLatestMessageReceived = msg.ToString();
                    break;

                // Handle Connect events.
                case NetworkEvent.Type.Connect:
                    Debug.Log("Player connected to the Host");
                    break;

                // Handle Disconnect events.
                case NetworkEvent.Type.Disconnect:
                    Debug.Log("Player got disconnected from the Host");
                    clientConnection = default(NetworkConnection);
                    break;
            }
        }
    }

    public byte[] GetReceivedData()
    {
        for (int i = 0; i<receivedDatas.Length; i++)
        {
            if (receivedDatas[i] == null)
            {
                continue;
            }
            else {
                byte[] receivedData = receivedDatas[i];
                receivedDatas[i] = null;
                return receivedData; 
            }
        }
        return null;
    }

    public void AppendReceivedData(byte[] data)
    {
        for (int i = 0; i < receivedDatas.Length; i++)
        {
            if (receivedDatas[i] == null)
            {
                receivedDatas[i] = data;
                break;
            }
        }
    }

}
