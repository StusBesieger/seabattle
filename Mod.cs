using System;
using System.Collections.Generic;
using Modding;
using Modding.Blocks;
using UnityEngine;
using UnityEngine.UI;


namespace StusNavalSpace
{
	public class Mod : ModEntryPoint
	{
		public static GameObject SNB_UI;    //Mod全体の管理用のGameObject
		public static Dictionary<int, int> HPDict;  //鯖内全員のHPが保存される辞書
		public static MessageType messageType1; //HPを送る
		public static Message message1;

		public static Message message2;

		public static ModAssetBundle modAssetBundle;

		public static void Log(string msg)
		{
			Debug.Log("SNB Log: " + msg);
		}
		public static void Warning(string msg)
		{
			Debug.LogWarning("SNB Warning: " + msg);
		}
		public static void Error(string msg)
		{
			Debug.LogError("SNB Error: " + msg);
		}

		public override void OnLoad()
		{
			// Called when the mod is loaded.

			//各ModuleとBehaviourをセットにし、XML上で使えるように
			Modding.Modules.CustomModules.AddBlockModule<SNBUIModule, SNBUIBehaviour>("SNBUIModule", true);
			Modding.Modules.CustomModules.AddBlockModule<SNBAmmoUIModule, SNBAmmoUIBehaviour>("SNBAmmoUIModule", true);
			Modding.Modules.CustomModules.AddBlockModule<SNBAdWeaponConfigModule, SNBAdWeaponConfigBehaviour>("SNBAdWeaponConfigModule", true);
			Modding.Modules.CustomModules.AddBlockModule<SNBEffectModule, SNBEffectBehaviour>("SNBEffectModule", true);
			Modding.Modules.CustomModules.AddBlockModule<SNBAdTropedConfiModule, SNBTropedBehaviour>("SNBAdTropedConfiModule", true);

			HPDict = new Dictionary<int, int> { };

			//SNB_UIを作成、Canvasを追加
			SNB_UI = new GameObject("SNB UI");
			UnityEngine.Object.DontDestroyOnLoad(SNB_UI);
			Canvas val = SNB_UI.AddComponent<Canvas>();
			val.renderMode = 0;
			val.sortingOrder = 0;
			val.gameObject.layer = LayerMask.NameToLayer("HUD");
			SNB_UI.AddComponent<CanvasScaler>().scaleFactor = 1f;   //画面サイズに応じてUIをスケーリングするためのコンポーネントをアタッチする


			//AssetBundleの読み込み
			switch (Application.platform)   //OS毎に変更
			{
				case RuntimePlatform.WindowsPlayer:
					modAssetBundle = ModResource.GetAssetBundle("SBNmyasset");
					break;
				case RuntimePlatform.OSXPlayer:
					modAssetBundle = ModResource.GetAssetBundle("SBNmyassetMac");
					break;
				case RuntimePlatform.LinuxPlayer:
					modAssetBundle = ModResource.GetAssetBundle("SBNmyassetMac");
					break;
				default:
					modAssetBundle = ModResource.GetAssetBundle("SBNmyasset");
					break;
			}


			//クライアントに送信するメッセージの型と受理時動作の登録
			messageType1 = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer);
			ModNetworking.Callbacks[messageType1] += new Action<Message>(ApplyHP);

		}

		public static void ApplyHP(Message message) //ホストから送られてきた値にHPを更新
		{
			HPDict[(int)message.GetData(0)] = (int)message.GetData(1);
		}

	}

}
