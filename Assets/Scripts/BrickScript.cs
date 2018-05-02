using UnityEngine;
using System.Collections;

public class BrickScript : MonoBehaviour {
	private int brickLife = 3;

	NetworkViewID myViewID;

	private void Start() {
		this.myViewID = GetComponent<NetworkView>().viewID;
	}

	public void SubtractLife() {
		brickLife--;

		if (GetComponent<NetworkView>().isMine) {
			if (brickLife <= 0) {
				GetComponent<NetworkView>().RPC("DestroyBrick", RPCMode.Server);
			}
		}
	}

	[RPC]
	private void DestroyBrick() {
		Network.RemoveRPCs(myViewID);
		Network.Destroy(this.gameObject);

		GetComponent<NetworkView>().RPC("UpdateDestroyBrick", RPCMode.Others);
	}

	[RPC]
	private void UpdateDestroyBrick() {
		Network.RemoveRPCs(myViewID);
		Network.Destroy(this.gameObject);
	}
}
