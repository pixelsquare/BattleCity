using UnityEngine;
using System.Collections;

public enum ObjectPosition { None, Top, Left, MiddleCenter, Center, Bottom, Right, Null };

public class PlayerController : MonoBehaviour {

	public float speed = 5.0f;
	public GameObject bullet;
	public Transform bulletSpawnPoint;
	public float rateOfFire = 1.0f;

	private bool fireIsReady = true;

	public Transform nameplateParent;
	public TextMesh nameplate;

	private bool isVertical = false;
	private bool isHorizontal = false;

	private Vector3 playerRotation;

	private Vector3 objectSize = new Vector3();
	private Vector3 viewportCoord = new Vector3();

	private Vector3 moveDirection = new Vector3();
	public Camera playerCamera;

	private NetworkPlayer owner;

	private ObjectPosition playerScreenPosition = ObjectPosition.Null;

	private void Update() {
		if (this.owner != Network.player) return;

		UpdateObjectPosition();

		if (!this.isHorizontal) {
			if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) {
				if (transform.position.z < 9.0f) {
					playerRotation = transform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);

					if (Network.isServer) GetComponent<NetworkView>().RPC("UpdateUserInput", RPCMode.All, this.playerRotation, this.moveDirection, this.isVertical, this.isHorizontal, (int)this.playerScreenPosition);
					if (Network.isClient) GetComponent<NetworkView>().RPC("SendUserInput", RPCMode.Server, this.playerRotation, this.moveDirection, this.isVertical, this.isHorizontal, (int)this.playerScreenPosition);

					this.isVertical = true;
					this.moveDirection = Vector3.up;
				}
			}
			else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) {
				if (transform.position.z > -9.0f) {
					playerRotation = transform.localEulerAngles = new Vector3(0.0f, 180.0f, 0.0f);

					if (Network.isServer) GetComponent<NetworkView>().RPC("UpdateUserInput", RPCMode.All, this.playerRotation, this.moveDirection, this.isVertical, this.isHorizontal, (int)this.playerScreenPosition);
					if (Network.isClient) GetComponent<NetworkView>().RPC("SendUserInput", RPCMode.Server, this.playerRotation, this.moveDirection, this.isVertical, this.isHorizontal, (int)this.playerScreenPosition);

					this.isVertical = true;
					this.moveDirection = Vector3.down;
				}
			}
			else {
				this.isVertical = false;
			}
		}

		if (!this.isVertical) {
			if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
				if (transform.position.x > -13.0f) {
					playerRotation = transform.localEulerAngles = new Vector3(0.0f, 270.0f, 0.0f);

					if (Network.isServer) GetComponent<NetworkView>().RPC("UpdateUserInput", RPCMode.All, this.playerRotation, this.moveDirection, this.isVertical, this.isHorizontal, (int)this.playerScreenPosition);
					if (Network.isClient) GetComponent<NetworkView>().RPC("SendUserInput", RPCMode.Server, this.playerRotation, this.moveDirection, this.isVertical, this.isHorizontal, (int)this.playerScreenPosition);

					this.isHorizontal = true;
					this.moveDirection = Vector3.left;
				}
			}
			else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
				if (transform.position.x < 13.0f) {
					playerRotation = transform.localEulerAngles = new Vector3(0.0f, 90.0f, 0.0f);

					if (Network.isServer) GetComponent<NetworkView>().RPC("UpdateUserInput", RPCMode.All, this.playerRotation, this.moveDirection, this.isVertical, this.isHorizontal, (int)this.playerScreenPosition);
					if (Network.isClient) GetComponent<NetworkView>().RPC("SendUserInput", RPCMode.Server, this.playerRotation, this.moveDirection, this.isVertical, this.isHorizontal, (int)this.playerScreenPosition);

					this.isHorizontal = true;
					this.moveDirection = Vector3.right;
				}
			}
			else {
				this.isHorizontal = false;
			}
		}

		if (Input.GetKeyDown(KeyCode.Space) && fireIsReady) {
			//StartCoroutine("Fire", this.rateOfFire);
			GetComponent<NetworkView>().RPC("FireBullet", RPCMode.All, Network.player);
		}
	}

	private void OnCollisionEnter(Collision col) {
		if (col.transform.tag == "Player") {
			Physics.IgnoreCollision(this.GetComponent<Collider>(), col.collider);
		}
	}

	private IEnumerator Fire(float time) {
		fireIsReady = false;
		GetComponent<NetworkView>().RPC("FireBullet", RPCMode.All, Network.player);
		yield return new WaitForSeconds(time);
		fireIsReady = true;
	}

	private void UpdateObjectPosition() {
		Vector3 objectCorner = gameObject.transform.position;
		Vector2 multiplier = new Vector2();

		MeshFilter objMeshFilter = gameObject.GetComponent<MeshFilter>();
		if (objMeshFilter != null) {
			objectSize = objMeshFilter.sharedMesh.bounds.size;
			objectSize.Scale(gameObject.transform.localScale);
		}
		else
			objectSize = gameObject.transform.localScale;

		if (moveDirection.x != 0.0f) {
			multiplier.x = moveDirection.x / Mathf.Abs(moveDirection.x);
			objectCorner.x += multiplier.x * objectSize.x / -2.0f;
		}

		if (moveDirection.y != 0.0f) {
			multiplier.y = moveDirection.y / Mathf.Abs(moveDirection.y);
			objectCorner.y += multiplier.y * objectSize.y / -2.0f;
		}

		viewportCoord = playerCamera.WorldToViewportPoint(objectCorner);

		Vector2 defaultSize = new Vector2(27.0f, 7.0f);
		Vector2 cameraOffset = new Vector2();
		cameraOffset.x = objectSize.x / defaultSize.x;
		cameraOffset.y = objectSize.y / defaultSize.y;

		cameraOffset.x = Mathf.Clamp(cameraOffset.x, 1.0f, 2.0f);
		cameraOffset.y = Mathf.Clamp(cameraOffset.y, 1.0f, 2.0f);

		Vector2 middleCenterMin = new Vector2();
		middleCenterMin.x = (cameraOffset.x * 0.5f) - 0.01f;
		middleCenterMin.y = (cameraOffset.y * 0.5f) - 0.01f;

		Vector2 middleCenterMax = new Vector2();
		middleCenterMax.x = (cameraOffset.x * 0.5f) + 0.01f;
		middleCenterMax.y = (cameraOffset.y * 0.5f) + 0.01f;

		if (multiplier.x != 0.0f) {
			if (viewportCoord.x > cameraOffset.x - 0.05f)
				playerScreenPosition = ObjectPosition.Right;
			else if (viewportCoord.x > middleCenterMin.x && viewportCoord.x < middleCenterMax.x)
				playerScreenPosition = ObjectPosition.MiddleCenter;
			else if (viewportCoord.x < 0.0f + 0.05f)
				playerScreenPosition = ObjectPosition.Left;
			else if (viewportCoord.x < middleCenterMin.x || viewportCoord.x > middleCenterMax.x)
				playerScreenPosition = ObjectPosition.Center;
			else
				playerScreenPosition = ObjectPosition.None;
		}

		if (multiplier.y != 0.0f) {
			if (viewportCoord.y > cameraOffset.y - 0.05f)
				playerScreenPosition = ObjectPosition.Top;
			else if (viewportCoord.y > middleCenterMin.y && viewportCoord.y < middleCenterMax.y)
				playerScreenPosition = ObjectPosition.MiddleCenter;
			else if (viewportCoord.y < 0.0f + 0.05f)
				playerScreenPosition = ObjectPosition.Bottom;
			else if (viewportCoord.y < middleCenterMin.y || viewportCoord.y > middleCenterMax.y)
				playerScreenPosition = ObjectPosition.Center;
			else
				playerScreenPosition = ObjectPosition.None;
		}
	}

	[RPC]
	public void SetOwner(NetworkPlayer player) {
		this.owner = player;
		Debug.Log("Setting Owner to " + this.owner);
	}

	[RPC]
	private void SendUserInput(Vector3 rotation, Vector3 moveDir, bool vert, bool horiz, int screenPos) {
		transform.position += transform.forward * speed * Time.deltaTime;
		transform.eulerAngles = rotation;
		this.bulletSpawnPoint.eulerAngles = rotation;
		this.moveDirection = moveDir;
		this.isVertical = vert;
		this.isHorizontal = horiz;
		this.playerScreenPosition = (ObjectPosition)screenPos;
		nameplateParent.transform.eulerAngles = Vector3.zero;

		GetComponent<NetworkView>().RPC("UpdateUserInput", RPCMode.Others, rotation, moveDir, vert, horiz, screenPos);
	}

	[RPC]
	private void UpdateUserInput(Vector3 rotation, Vector3 moveDir, bool vert, bool horiz, int screenPos) {
		transform.position += transform.forward * speed * Time.deltaTime;
		transform.eulerAngles = rotation;
		this.bulletSpawnPoint.eulerAngles = rotation;
		this.moveDirection = moveDir;
		this.isVertical = vert;
		this.isHorizontal = horiz;
		this.playerScreenPosition = (ObjectPosition)screenPos;
		nameplateParent.transform.eulerAngles = Vector3.zero;
	}

	[RPC]
	private void FireBullet(NetworkPlayer player) {
		if(Network.isServer) {
			GameObject bulletInstance = (GameObject)Network.Instantiate(this.bullet, this.bulletSpawnPoint.position, this.bulletSpawnPoint.rotation, NetworkGroup.BULLET_GROUP);

			Vector3 tmpBulletDir = moveDirection;
			if (tmpBulletDir.y != 0.0f) {
				tmpBulletDir.z = tmpBulletDir.y;
				tmpBulletDir.y = 0.0f;
			}

			NetworkView nView = bulletInstance.GetComponent<NetworkView>();
			nView.RPC("SetBulletID", RPCMode.All, player);
			nView.RPC("SetMoveDirection", RPCMode.All, tmpBulletDir);
			nView.RPC("SetMainTank", RPCMode.All, this.GetComponent<NetworkView>().viewID);

		}
	}

	public NetworkPlayer GetOwner() {
		return this.owner;
	}
}
