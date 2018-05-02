using UnityEngine;
using System.Collections;
using System.Collections.Generic;

enum GameManager { MainMenu, Waiting, MainGame };

[RequireComponent(typeof(NetworkView))]
public class NetworkManager : MonoBehaviour {
	private string ipAddress;
	private string port;

	const int MAX_PLAYER_CONNECTION = 4;
	const int MAX_NAME_LENGTH = 6;

	private GameManager gameManager;

	private Rect mainMenuRect;
	private Rect waitingRect;
	private Rect playerListRect;

	private bool showPlayerListWindow = true;

	private GUIStyle gameManagerTextStyle = new GUIStyle();

	private List<PlayerInformation> playerInfo = new List<PlayerInformation>();
	private string tmpName;
	public GameObject map;
	public List<GameObject> playerTanks;
	public List<Transform> spawnPoints;

	private int currConnections;

	private void OnServerInitialized() {
		Debug.Log("Server Initialized!");

		this.gameManager = GameManager.Waiting;

		PlayerInformation serverInfo = new PlayerInformation();
		serverInfo.name = this.tmpName;
		serverInfo.playerID = Network.player;
		playerInfo.Add(serverInfo);
	}

	private void OnPlayerConnected(NetworkPlayer player) {
		Debug.Log("I'm Connected! (Called from the SERVER)");
		GetComponent<NetworkView>().RPC("UpdatePlayerConnectionStatus", RPCMode.AllBuffered, 1);
	}

	private void OnPlayerDisconnected(NetworkPlayer player) {
		Debug.Log("I'm Disconnected! (Called from the SERVER!");
		GetComponent<NetworkView>().RPC("UpdatePlayerConnectionStatus", RPCMode.All, -1);
	}

	private void OnConnectedToServer() {
		Debug.Log("I'm Connected! (Called from the CLIENT)");
		this.gameManager = GameManager.Waiting;
		GetComponent<NetworkView>().RPC("AddPlayer", RPCMode.Server, Network.player, this.tmpName);
	}

	private void OnDisconnectedFromServer(NetworkDisconnection info) {
		Debug.Log("I'm Disconnected! (Called from the CLIENT!");
		this.gameManager = GameManager.MainMenu;
		
	}

	private void Start() {
		this.ipAddress = "127.0.0.1";
		this.port = "984364";

		this.gameManager = GameManager.MainMenu;

		this.mainMenuRect = new Rect((Screen.width - 250.0f) * 0.5f, (Screen.height - 200.0f) * 0.5f, 250.0f, 130.0f);
		this.waitingRect = new Rect((Screen.width - 500.0f) * 0.5f, (Screen.height - 150.0f) * 0.5f, 500.0f, 150.0f);
		this.playerListRect = new Rect((Screen.width - 130.0f) * 0.05f, (Screen.height - 150.0f) * 0.05f, 130.0f, 150.0f);

		this.gameManagerTextStyle.normal.textColor = Color.white;
		this.gameManagerTextStyle.fontStyle = FontStyle.BoldAndItalic;
		this.gameManagerTextStyle.fontSize = 14;

		this.tmpName = "";

		this.currConnections = 0;
	}

	private void Update() {
		if (this.gameManager == GameManager.MainGame) {
			Invoke("CheckTankTime", 1.0f);
		}
	}

	private void CheckTankTime() {
		int i = 0;
		foreach (PlayerInformation element in playerInfo) {
			if (element.tank == null) {
				if (element.spawnTime > 0.0f) {
					element.spawnTime -= Time.deltaTime;
				}

				if (element.spawnTime < 1.0f) {
					GetComponent<NetworkView>().RPC("RespawnPlayer", RPCMode.All, element.playerID, i);
				}
			}
			i++;
		}
	}

	private void OnGUI() {
		switch (this.gameManager) {
			case GameManager.MainMenu:
				this.WindowText("Main Menu");
				this.mainMenuRect = GUILayout.Window(0, this.mainMenuRect, MainMenuWindow, "Menu");
				break;

			case GameManager.Waiting:
				if (Network.isServer) this.WindowText("Waiting (SERVER)");
				if (Network.isClient) this.WindowText("Waiting (CLIENT)");

				this.waitingRect = GUILayout.Window(1, this.waitingRect, WaitingWindow, "Status");
				break;

			case GameManager.MainGame:
				if (Network.isServer) this.WindowText("Main Game (SERVER)");
				if (Network.isClient) this.WindowText("Main Game (CLIENT)");

				if(showPlayerListWindow)
					this.playerListRect = GUILayout.Window(2, this.playerListRect, MainGameWindow, "");
				else
					this.playerListRect = GUILayout.Window(2, this.playerListRect, MainGameWindow, "", GUILayout.Height(10.0f));
				break;
		}
	}

	private void MainMenuWindow(int id) {
		GUIStyle centerText = new GUIStyle();
		centerText.fontStyle = FontStyle.Bold;
		centerText.normal.textColor = Color.white;
		centerText.richText = true;

		if (Network.peerType == NetworkPeerType.Disconnected) {
			GUILayout.BeginHorizontal();
			GUILayout.Label("Name: ", centerText);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			this.tmpName = GUILayout.TextField(this.tmpName, MAX_NAME_LENGTH);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("IP Address: ", centerText);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			this.ipAddress = GUILayout.TextField(this.ipAddress);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Port: ", centerText);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			this.port = GUILayout.TextField(this.port);
			GUILayout.EndHorizontal();

		
			if (GUILayout.Button("New Server")) {
				Network.InitializeServer(MAX_PLAYER_CONNECTION, int.Parse(port), true);

			}
			if (GUILayout.Button("Connect Server")) {
				Network.Connect(this.ipAddress, int.Parse(this.port));
				
			}

			if (GUILayout.Button("Exit")) {
				Application.Quit();
			}
		}
	}

	private void WaitingWindow(int id) {
		if (this.currConnections == (MAX_PLAYER_CONNECTION - 1)) {
			if (Network.isServer) {
				GUILayout.FlexibleSpace();

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Game is Ready! (" + (this.currConnections + 1) + "/" + MAX_PLAYER_CONNECTION + ")");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				GUILayout.FlexibleSpace();

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Start Game")) {
					GetComponent<NetworkView>().RPC("StartGame", RPCMode.All);
				}

				if (GUILayout.Button("Cancel")) {
					Network.Disconnect();
					this.gameManager = GameManager.MainMenu;
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}

			if (Network.isClient) {
				GUILayout.FlexibleSpace();

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Game is Ready! (" + (this.currConnections + 1) + "/" + MAX_PLAYER_CONNECTION + ")");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Waiting for host to start the game.");
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				GUILayout.FlexibleSpace();

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Cancel")) {
					Network.Disconnect();
					this.gameManager = GameManager.MainMenu;
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
		}
		else {
			GUILayout.FlexibleSpace();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label("Waiting for other players (" + (this.currConnections + 1) + "/" + MAX_PLAYER_CONNECTION + ") ..");
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.FlexibleSpace();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Cancel")) {
				Network.Disconnect();
				this.gameManager = GameManager.MainMenu;
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
	}

	private void MainGameWindow(int id) {
		if (showPlayerListWindow) {
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label("Player");
			GUILayout.FlexibleSpace();

			GUILayout.FlexibleSpace();
			GUILayout.FlexibleSpace();

			GUILayout.FlexibleSpace();
			GUILayout.Label("K");
			GUILayout.Label("\\");
			GUILayout.Label("D");
			GUILayout.FlexibleSpace();

			GUILayout.EndHorizontal();

			GUIStyle userProperties = new GUIStyle();
			userProperties.normal.textColor = Color.white;
			userProperties.fontStyle = FontStyle.Bold;
			userProperties.fontSize = 13;

			foreach (PlayerInformation element in playerInfo) {

				if (element.spawnTime > 0.0f) {
					GUILayout.BeginHorizontal();
					if (element.playerID == Network.player) 
						GUILayout.Label(" (" + (int)element.spawnTime + ")", userProperties);
					else
						GUILayout.Label(" (" + (int)element.spawnTime + ")");

					GUILayout.FlexibleSpace();

					if (element.playerID == Network.player) 
						GUILayout.Label(element.name, userProperties);
					else 
						GUILayout.Label(element.name);
					GUILayout.FlexibleSpace();

					if (element.name.Length < 3) {
						GUILayout.FlexibleSpace();
					}

					GUILayout.FlexibleSpace();
					if (element.playerID == Network.player)
						GUILayout.Label(element.kills + "", userProperties);
					else
						GUILayout.Label(element.kills + "");

					if (element.playerID == Network.player)
						GUILayout.Label("  \\  ", userProperties);
					else
						GUILayout.Label("\\");

					if (element.playerID == Network.player)
						GUILayout.Label(element.deaths + "", userProperties);
					else
						GUILayout.Label(element.deaths + "");
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
				}
				else {
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					if (element.playerID == Network.player) 
						GUILayout.Label(element.name, userProperties);
					else 
						GUILayout.Label(element.name);
					GUILayout.FlexibleSpace();

					if (element.name.Length < 3) {
						GUILayout.FlexibleSpace();
					}

					GUILayout.FlexibleSpace();
					if (element.playerID == Network.player) 
						GUILayout.Label(element.kills + " ", userProperties);
					else
						GUILayout.Label(element.kills + "");

					if (element.playerID == Network.player) 
						GUILayout.Label("  \\  ", userProperties);
					else
						GUILayout.Label("\\");

					if (element.playerID == Network.player) 
						GUILayout.Label(" " + element.deaths, userProperties);
					else 
						GUILayout.Label(element.deaths + "");
					GUILayout.FlexibleSpace();

					GUILayout.EndHorizontal();
				}
			}
		}

		GUILayout.FlexibleSpace();

		if (showPlayerListWindow)
			showPlayerListWindow = GUILayout.Toggle(showPlayerListWindow, "Hide Window");
		else
			showPlayerListWindow = GUILayout.Toggle(showPlayerListWindow, "Show Window");

		GUI.DragWindow();
	}

	private void WindowText(string text) {
		GUILayout.BeginArea(new Rect(0.0f, 0.0f, Screen.width, Screen.height));

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(text, this.gameManagerTextStyle);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.EndArea();
	}

	[RPC]
	private void UpdatePlayerConnectionStatus(int num) {
		this.currConnections += num ;
	}

	[RPC]
	private void AddPlayer(NetworkPlayer player, string playerName) {
		PlayerInformation tmpInfo = new PlayerInformation();
		tmpInfo.name = playerName;
		tmpInfo.playerID = player;
		playerInfo.Add(tmpInfo);

		foreach (PlayerInformation element in playerInfo) {
			GetComponent<NetworkView>().RPC("UpdatePlayerList", RPCMode.Others, element.playerID, element.name);
		}
	}

	[RPC]
	private void UpdatePlayerList(NetworkPlayer player, string playerName) {
		foreach (PlayerInformation element in playerInfo) {
			if (element.playerID == player && element.name == playerName) {
				return;
			}
		}

		PlayerInformation tmpInfo = new PlayerInformation();
		tmpInfo.name = playerName;
		tmpInfo.kills = 0;
		tmpInfo.deaths = 0;
		tmpInfo.spawnTime = 0.0f;
		tmpInfo.playerID = player;
		playerInfo.Add(tmpInfo);
	}

	[RPC]
	private void StartGame() {
		this.gameManager = GameManager.MainGame;

		GetComponent<NetworkView>().RPC("SpawnPlayers", RPCMode.All, Network.player);
	}

	[RPC]
	private void SpawnPlayers(NetworkPlayer player) {
		if (Network.isServer) {
			int i = 0;
			foreach (PlayerInformation element in playerInfo) {
				if (element.playerID == player) {
					element.tank = (GameObject)Network.Instantiate(playerTanks[i], spawnPoints[i].position, spawnPoints[i].localRotation, NetworkGroup.PLAYER_GROUP);

					PlayerController playerControl = element.tank.GetComponent<PlayerController>();
					playerControl.nameplate.text = element.name;
					element.tank.name = element.playerID + " " + element.name;
					element.playerID = player;
					element.spawnTime = 0.0f;
					element.playerIndx = i;

					NetworkView nView = element.tank.GetComponent<NetworkView>();
					nView.RPC("SetOwner", RPCMode.AllBuffered, element.playerID);
				}
				i++;
			}
			GetComponent<NetworkView>().RPC("UpdatePlayerNames", RPCMode.Others);
		}
	}

	[RPC]
	private void UpdatePlayerNames() {
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

		int i = 0;
		foreach (GameObject player in players) {
			PlayerController playerControl = player.GetComponent<PlayerController>();
			if (playerControl.GetOwner() != playerInfo[i].playerID) {
				i++;
			}
			else {
				playerControl.nameplate.text = playerInfo[i].name;
				player.name = playerInfo[i].playerID + " " + playerInfo[i].name;
				playerInfo[i].tank = player;
				playerInfo[i].playerIndx = i;
				playerInfo[i].spawnTime = 0.0f;
				i++;
			}
		}
	}

	[RPC]
	private void RespawnPlayer(NetworkPlayer playerID, int indx) {
		if (Network.isServer) {
			int i = 0;
			foreach (PlayerInformation element in playerInfo) {
				if (element.playerID == playerID && element.tank == null) {
					element.tank = (GameObject)Network.Instantiate(playerTanks[i], spawnPoints[i].position, spawnPoints[i].localRotation, NetworkGroup.PLAYER_GROUP);

					PlayerController playerControl = element.tank.GetComponent<PlayerController>();
					playerControl.nameplate.text = element.name;
					element.tank.name = element.playerID + " " + element.name;
					element.playerID = playerID;
					element.spawnTime = 0.0f;
					element.playerIndx = i;

					NetworkView nView = element.tank.GetComponent<NetworkView>();
					nView.RPC("SetOwner", RPCMode.AllBuffered, element.playerID);
				}
				i++;
			}

			GetComponent<NetworkView>().RPC("UpdateRespawnPlayer", RPCMode.Others, playerID, indx);
		}
	}

	[RPC]
	private void UpdateRespawnPlayer(NetworkPlayer playerID, int indx) {
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

		foreach (GameObject player in players) {
			PlayerController playerControl = player.GetComponent<PlayerController>();

			if (playerControl.GetOwner() == playerID) {
				player.name = playerInfo[indx].playerID + " " + playerInfo[indx].name;
				playerControl.nameplate.text = playerInfo[indx].name;
				playerInfo[indx].tank = player;
				playerInfo[indx].playerID = playerID;
				playerInfo[indx].spawnTime = 0.0f;
				playerInfo[indx].playerIndx = indx;
			}
		}
	}

	[RPC]
	public void AddKills(NetworkPlayer id) {
		int tmpKills = 0;
		foreach (PlayerInformation element in playerInfo) {
			if (element.playerID == id) {
				tmpKills = element.kills;
				tmpKills++;
			}
		}

		GetComponent<NetworkView>().RPC("UpdateKills", RPCMode.Others, id, tmpKills);
	}

	[RPC]
	private void UpdateKills(NetworkPlayer id, int killCount) {
		foreach (PlayerInformation element in playerInfo) {
			if (element.playerID == id) {
				element.kills = killCount;
			}
		}
	}

	[RPC]
	public void AddDeaths(NetworkPlayer id) {
		int tmpDeaths = 0;
		foreach (PlayerInformation element in playerInfo) {
			if (element.playerID == id) {
				tmpDeaths = element.deaths;
				tmpDeaths++;
			}
		}

		GetComponent<NetworkView>().RPC("UpdateDeaths", RPCMode.Others, id, tmpDeaths);
	}

	[RPC]
	private void UpdateDeaths(NetworkPlayer id, int deathCount) {
		foreach (PlayerInformation element in playerInfo) {
			if (element.playerID == id) {
				element.deaths = deathCount;
			}
		}
	}

	[RPC]
	public void AddSpawnTime(NetworkPlayer id) {
		float tmpSpawnTime = 0.0f;
		foreach (PlayerInformation element in playerInfo) {
			if (element.playerID == id) {
				tmpSpawnTime = element.spawnTime += 5.0f;
			}
		}

		GetComponent<NetworkView>().RPC("UpdateSpawnTime", RPCMode.Others, id, tmpSpawnTime);
	}

	[RPC]
	private void UpdateSpawnTime(NetworkPlayer id, float spawnTime) {
		foreach (PlayerInformation element in playerInfo) {
			if (element.playerID == id) {
				element.spawnTime = spawnTime;
			}
		}
	}
}

[System.Serializable]
public class PlayerInformation {
	public string name;
	public int kills = 0;
	public int deaths = 0;
	public float spawnTime = 0.0f;
	public int playerIndx = 0;
	public NetworkPlayer playerID;
	public GameObject tank;
}