using UnityEngine;
using System.Collections;

public class BulletScript : MonoBehaviour {

	public float speed = 5;
	public GameObject[] destroyParticle;

	private NetworkManager networkManager;
	private NetworkView networkManagerNView;

	private NetworkPlayer ID;
	private NetworkViewID mainTankID;

	private Vector3 newRigidbodyPos;
	private Vector3 newRigidbodyVel;
	private Quaternion newRigidbodyRot;

	private Vector3 bulletMoveDir;

	private bool didHit = false;

	private NetworkViewID bulletViewID;

	private void Start() {
		GetComponent<NetworkView>().observed = this;

		transform.localScale = Vector3.one * 0.2f;
		bulletViewID = this.gameObject.GetComponent<NetworkView>().viewID;

		networkManager = GameObject.FindGameObjectWithTag("Network Manager").GetComponent<NetworkManager>();
		networkManagerNView = networkManager.GetComponent<NetworkView>();

		this.newRigidbodyPos = Vector3.zero;
		this.newRigidbodyVel = Vector3.zero;
		this.newRigidbodyRot = Quaternion.identity;
	}

	private void Update() {
		transform.position += this.bulletMoveDir * speed * Time.deltaTime;

		if (CheckBorders(transform.position)) {
			Network.RemoveRPCs(bulletViewID.owner, NetworkGroup.BULLET_GROUP);
			Network.Destroy(bulletViewID);
			Destroy(this.gameObject);
		}
	}

	private bool CheckBorders(Vector3 pos) {
		if (pos.x < -14.0f || pos.x > 14.0f || pos.z < -11.0f || pos.z > 11.0f)
			return true;

		return false;
	}

	private void OnTriggerEnter(Collider col) {
		NetworkView enemyNetworkView = col.gameObject.GetComponent<NetworkView>();
		NetworkViewID enemyViewID = enemyNetworkView.viewID;
	
		if (enemyViewID != mainTankID) {
			if (col.tag == "Player" && !didHit) {
				if (GetComponent<NetworkView>().isMine) {
					networkManagerNView.RPC("AddKills", RPCMode.Server, this.ID);
					networkManagerNView.RPC("AddDeaths", RPCMode.Server, col.GetComponent<PlayerController>().GetOwner());
					networkManagerNView.RPC("AddSpawnTime", RPCMode.Server, col.GetComponent<PlayerController>().GetOwner());

					GetComponent<NetworkView>().RPC("DestroyTarget", RPCMode.Server, enemyViewID, col.tag);
					GetComponent<NetworkView>().RPC("DestroyBullet", RPCMode.Server, bulletViewID);
					didHit = true;
				}
			}

			if (col.tag == "Brick" && !didHit) {
				if (GetComponent<NetworkView>().isMine) {
					col.GetComponent<BrickScript>().SubtractLife();
					GetComponent<NetworkView>().RPC("DestroyBullet", RPCMode.Server, bulletViewID);
					didHit = true;
				}
			}

			if (col.tag == "Wall" && !didHit) {
				if (GetComponent<NetworkView>().isMine) {
					GetComponent<NetworkView>().RPC("DestroyBullet", RPCMode.Server, bulletViewID);
					didHit = true;
				}
			}
		}
	}

	[RPC]
	private void DestroyBullet(NetworkViewID bulletID) {
		Network.RemoveRPCs(bulletID.owner, NetworkGroup.BULLET_GROUP);
		Network.Destroy(bulletID);

		GetComponent<NetworkView>().RPC("UpdateDestroyBullet", RPCMode.Others, bulletID);
	}

	[RPC]
	private void UpdateDestroyBullet(NetworkViewID bulletID) {
		Network.RemoveRPCs(bulletID.owner, NetworkGroup.BULLET_GROUP);
		Network.Destroy(bulletID);
	}

	[RPC]
	private void DestroyTarget(NetworkViewID target, string tag) {
		if(tag == "Player") {
			Network.RemoveRPCs(target.owner, NetworkGroup.PLAYER_GROUP);
		}

		Network.RemoveRPCs(target);
		Network.Destroy(target);

		GetComponent<NetworkView>().RPC("UpdateDestroyTarget", RPCMode.Others, target, tag);
	}

	[RPC]
	private void UpdateDestroyTarget(NetworkViewID target, string tag) {
		if (tag == "Player") {
			Network.RemoveRPCs(target.owner, NetworkGroup.PLAYER_GROUP);
		}

		Network.RemoveRPCs(target);
		Network.Destroy(target);
	}

	[RPC]
	private void SetMoveDirection(Vector3 moveDir) {
		this.bulletMoveDir = moveDir;
	}

	[RPC]
	public void SetBulletID(NetworkPlayer id) {
		ID = id;
	}

	[RPC]
	public void SetMainTank(NetworkViewID tankID) {
		this.mainTankID = tankID;
	}

	public void SetNetworkManager(NetworkManager networkManage) {
		this.networkManager = networkManage;
	}

	private void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) {
		if (stream.isWriting) {
			Vector3 rigidbodyPos = GetComponent<Rigidbody>().position;
			Vector3 rigidbodyVel = GetComponent<Rigidbody>().velocity;
			Quaternion rigidbodyRot = GetComponent<Rigidbody>().rotation;

			stream.Serialize(ref rigidbodyPos);
			stream.Serialize(ref rigidbodyVel);
			stream.Serialize(ref rigidbodyRot);
		}
		else {
			stream.Serialize(ref newRigidbodyPos);
			stream.Serialize(ref newRigidbodyVel);
			stream.Serialize(ref newRigidbodyRot);

			this.GetComponent<Rigidbody>().position = newRigidbodyPos;
			this.GetComponent<Rigidbody>().velocity = newRigidbodyVel;
			this.GetComponent<Rigidbody>().rotation = newRigidbodyRot;
		}
	}
}
