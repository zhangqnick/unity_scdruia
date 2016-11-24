#pragma strict
import UnityEngine.UI;
import UnityEngine.SceneManagement;

enum SyncMode {
	CTRL_TO_VR,
	VR_TO_CTRL,
	INDIVIDUAL
}

private var ifpCsInterface : IFP_CsInterface;
ifpCsInterface = GetComponent(IFP_CsInterface);
private var ifpController : IFP_Controller;
ifpController = GetComponent(IFP_Controller);
private var ifpNetController : IFP_NetController;
ifpNetController = GetComponent(IFP_NetController);

private var isPersonalMode : boolean = false;
private var isTransFollowMode : boolean = false;

private var syncMode : int;

private var prevMousePos : Vector3 = new Vector3( 0f, 0f, 0f );
private var pointerPosition : Vector2 = new Vector2( -1000f, -1000f );

private	var prevPos : Vector3 = new Vector3( 0.0f, 0.0f, 0.0f );
private	var prevEuler : Vector3 = new Vector3( 0.0f, 0.0f, 0.0f );

//
public var imagegame : GameObject;
public var textName : Text;
public var UIcanve : GameObject;
public var Arcam : GameObject;


function Start () {
}

function OnDisable() {
	if(ifpNetController)
		ifpNetController.disconectNetwork();
	//Application.Quit();
}


function initAllPrms() {
	ifpController.initCtrlPrms();
	ifpNetController.initNetPrms();
	ifpCsInterface.csiTPTUIResetPrms();
}

function split2Strings(str : String, c : String) {
	var split = new String[2];
	var n : int = str.IndexOf(c);
	if (0 < n) {
		split[0] = str.Substring(0, n);
		if (n == (str.Length - 1))
			split[1] = "";
		else
			split[1] = str.Substring(n+1, str.Length-(n+1));
	}
	return split;
}

function getSplitCommandStr(str : String) {
	var split = new String[2];
	split[0] = str;
	split[1] = "";
	var n : int = str.IndexOf(":");
	if (0 < n) {
		split[0] = str.Substring(0, n);
		if (n == (str.Length - 1))
			split[1] = "";
		else
			split[1] = str.Substring(n+1, str.Length-(n+1));
	}
	return split;
}

function getCommandName(str : String) {
	var split : String[];
	split = str.Split(","[0]);
	return split;
}

function getParameterValues(str : String) {
	var split : String[];
	split = str.Split(","[0]);
	return split;
}

function printStr(str : String[]) {
	for (var i : int = 0; i < str.length; i++) {
		print (str[i]);
	}
}

function execCommand(com : String) {
	var com_str : String[];
	var com_name : String[];
	var com_prm : String[];

	com_str = getSplitCommandStr(com);
	com_name = getCommandName(com_str[0].ToLower());
	com_prm = getParameterValues(com_str[1]);

	var isDebugMode : int;
	isDebugMode = PlayerPrefs.GetInt("debug_mode");

	var btn : TPTabletUIButton = null;
	var grp : TPTabletUIGroup = null;
	var num : int;
	var snum : int;

	switch (com_name[0]) {
		case "show_button":
		case "show_botton":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("show_button");
		
			if (com_prm.Length < 2)
				break;
			btn = ifpCsInterface.csiTPTUIConfFindButtonByName(com_prm[0]);
			if (btn) {
				if (com_prm[1].ToLower() == "false")
					btn.SetDataSelection(-1);
				else if (com_prm[1].ToLower() == "true")
					btn.SetDataSelection(0);
			}
			break;
		case "switch_button":
		case "switch_botton":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("switch_button");
				
			if (com_prm.Length < 2)
				break;
			btn = ifpCsInterface.csiTPTUIConfFindButtonByName(com_prm[0]);
			/****/
			if (btn) {
				try {
					num = parseInt(com_prm[1]);
				} catch (e) {
					num = -10;
				}
				if (-1 <= num && num < btn.GetNumData()) {
					btn.SetDataSelection(num);
				}
			}
			break;
		case "switch_next_button":
		case "switch_next_botton":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("switch_next_button");
				
			if (com_prm.Length < 1)
				break;
			btn = ifpCsInterface.csiTPTUIConfFindButtonByName(com_prm[0]);
			if (btn) {
				snum = btn.GetDataSelection() + 1;
				if (btn.GetNumData() <= snum)
					snum = 0;
				btn.SetDataSelection(snum);
			}
			break;
		case "switch_prev_button":
		case "switch_prev_botton":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("switch_prev_button");

			if (com_prm.Length < 1)
				break;
			btn = ifpCsInterface.csiTPTUIConfFindButtonByName(com_prm[0]);
			if (btn) {
				snum = btn.GetDataSelection() - 1;
				if (snum < 0)
					snum = btn.GetNumData() - 1;
				btn.SetDataSelection(snum);
			}
			break;
		case "show_group":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("show_group");

			if (com_prm.Length < 2)
				break;
			grp = ifpCsInterface.csiTPTUIConfFindGroupByName(com_prm[0]);
			if (grp) {
				var show : boolean;
				if (com_prm[1].ToLower() == "true")
					show = true;
				else if (com_prm[1].ToLower() == "false")
					show = false;
				if ((com_prm[1].ToLower() == "true") || (com_prm[1].ToLower() == "false"))
					grp.SetEnable(show);
				if( isDebugMode == 1 )
					ifpCsInterface.csiTPTUIMngPutErrorMessage("Succeeded find group : " + com_prm[0] );
			}
			break;
		case "switch_child_group":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("switch_child_group");
		
			if (com_prm.Length < 2)
				break;
			grp = ifpCsInterface.csiTPTUIConfFindGroupByName(com_prm[0]);
			///////////
			if (grp) {
				var cgrp : TPTabletUIGroup;
				var n : int = grp.GetNumChildGroups();
				num = -10;
				for (var i : int = 0; i < n; i++) {
					cgrp = grp.GetChildGroup(i);
					if (com_prm[1].ToLower() == cgrp.GetName().ToLower()) {
						num = i;
						break;
					}
				}
				if (num < 0) {
					try {
						num = parseInt(com_prm[1]);
					} catch (e) {
						num = -10;
					}
				}
				if (-1 <= num && num < grp.GetNumChildGroups()) {
					if( isDebugMode == 1 )
						ifpCsInterface.csiTPTUIMngPutErrorMessage("Select child group : " + num );

					grp.SetChildGroupSelection(num);
				}
			}
			break;
		case "switch_next_child_group":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("switch_next_child_group");

			if (com_prm.Length < 1)
				break;
			grp = ifpCsInterface.csiTPTUIConfFindGroupByName(com_prm[0]);
			if (grp) {
				snum = grp.GetChildGroupSelection() + 1;
				if (grp.GetNumChildGroups() <= snum)
					snum = 0;
				grp.SetChildGroupSelection(snum);
			}
			break;
		case "switch_prev_child_group":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("switch_prev_child_group");

			if (com_prm.Length < 1)
				break;
			grp = ifpCsInterface.csiTPTUIConfFindGroupByName(com_prm[0]);
			if (grp) {
				snum = grp.GetChildGroupSelection() - 1;
				if (snum < 0)
					snum = grp.GetNumChildGroups() - 1;
				grp.SetChildGroupSelection(snum);
			}
			break;
		/******/
		case "change_seldata_text":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("change_seldata_text");

			if (com_prm.Length < 1)
				break;
			btn = ifpCsInterface.csiTPTUIConfFindButtonByName(com_prm[0]);
			if (btn) {
				var btnstr : String[];
				btnstr = split2Strings(com_str[1], ",");
				var selid : int = btn.GetDataSelection();
				//btn.SetDataTextString(selid, btnstr[1]);
				//print ("cmd = [" + btnstr[1] + "]");
				var changeSelDataTxt : String;
				changeSelDataTxt = btnstr[1].Substring( 0, btnstr[1].LastIndexOf(",") );
				changeSelDataTxt = changeSelDataTxt.Replace("\\n", "\n");
				btn.SetDataTextString(selid, changeSelDataTxt);
				if( isDebugMode == 1 )
					ifpCsInterface.csiTPTUIMngPutErrorMessage("id = " + selid + ", " + "cmd = [" + changeSelDataTxt + "]");
			}
			break;
		case "change_text":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("change_text");

			if (com_prm.Length < 2)
				break;
			btn = ifpCsInterface.csiTPTUIConfFindButtonByName(com_prm[0]);
			if (btn) {
				try {
					num = parseInt(com_prm[1]);
				} catch (e) {
					num = -1;
				}
				var txt1 : String[];
				var txt2 : String[];
				if (0 <= num && num < btn.GetNumData()) {
					txt1 = split2Strings(com_str[1], ",");
					txt2 = split2Strings(txt1[1], ",");
					//btn.SetDataTextString(num, txt2[1]);
					//print ("id = " + num + ", " + "cmd = [" + txt2[1] + "]");
					var changeTxt : String;
					changeTxt = txt2[1].Substring( 0, txt2[1].LastIndexOf(",") );
					changeTxt = changeTxt.Replace("\\n", "\n");
					btn.SetDataTextString(num, changeTxt);
					if( isDebugMode == 1 )
						ifpCsInterface.csiTPTUIMngPutErrorMessage("id = " + num + ", " + "cmd = [" + changeTxt + "]");
					//print ("id = " + num + ", " + "cmd = [" + strTxt + "]");
				}
			}
			break;
		/******/
		case "exit_app":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("exit_app");

			OnDisable();
			break;
		case "reset_prms":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("reset_prms");

			initAllPrms();
			break;
		case "net_status":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("net_status");

			break;
		case "net_auto_connect":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("net_auto_connect");

			var stat : boolean = ifpNetController.GetAutoConnect();
			if (com_prm[0].ToLower() == "true")
				stat = true;
			else if (com_prm[0].ToLower() == "false")
				stat = false;
		/****/
			ifpNetController.SetAutoConnect(stat);
			break;
		case "net_connect":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("net_connect");

			ifpNetController.conectNetwork();
			break;
		case "net_disconnect":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("net_disconnect");

			ifpNetController.disconectNetwork();
			ifpNetController.SetAutoConnect(false);
			break;
		case "send_message":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("send_message : " + com_str[1]);

			ifpNetController.sendMessage(com_str[1]);
			break;
		case "set_camera_trans":
			if( isDebugMode == 1 )
				ifpCsInterface.csiTPTUIMngPutErrorMessage("set_camera_trans : " + com_str[1]);
			setCameraTrans( com_str[1] );
			break;
		//case "change_scene":
		//	if( isDebugMode == 1 )
		//	    ifpCsInterface.csiTPTUIMngPutErrorMessage("shange_scene : " + com_str[1]);
		//	changeScene( com_str[1] );
		//	break;
		case "display_image":
		case "display_movie":
		case "image_control":
		case "display_content":
		case "animation_control":
		case "zoom":
		if(isDebugMode == 1)
		{
		    if(com_prm[0] == "true")
		    {		        
		        Application.LoadLevel ("vrtest");
		    }
		    else if(com_prm[0] == "false")
		    {
		        Application.LoadLevel ("study");
		    }
		}
		break;
		case "zoom_up":
		case "zoom_down":
		case "move_viewpoint":
		case "rotate_viewpoint":
		case "arcam":
		if( isDebugMode == 1 )
		{
		    if(com_prm[0] == "true")
		    {		        
		        Arcam.SetActive(true);
		        UIcanve.SetActive(false);
		    }
		    else if(com_prm[0] == "false")
		    {
		        Arcam.SetActive(false);
		        UIcanve.SetActive(true);
		    }
		}
		break;
		case "background":
		case "exit_contents":
			ifpNetController.sendMessage(com);
			break;
        case "gugong":
             if( isDebugMode == 1 )
             {
                 imagegame.SetActive(true);
                 StartCoroutine(WaitAndPrint(10.0)); 
             }
        case "emperor":
             if( isDebugMode == 1 )
             {
                 textName.text = com_prm[0];
             }
		default:
			if( isDebugMode == 1 )
			{
			    ifpCsInterface.csiTPTUIMngPutErrorMessage("RecieveMessage : " + com_str[0]  + " " + com_str[1]);
			    Debug.Log("RecieveMessage : " + com_str[0]  + " " + com_str[1]);
			}
			break;
	}
}

			function WaitAndPrint (waitTime : float) {
			    // suspend execution for waitTime seconds
			    // ÔÝÍ£Ö´ÐÐwaitTimeÃë
			    yield WaitForSeconds (waitTime);
			    imagegame.SetActive(false);
			}

function doEvents(evt : TPTabletUIEvent) {
	if (evt == null)
		return;
	var com : String;
	var n : int = evt.GetNumCommands();
	for (var i : int = 0; i < n; i++) {
		com = evt.GetCommand(i);
		execCommand(com);
	}
}

function doMoveEvents(evt : TPTabletUIEvent, state : int, pos_x : int, pos_y : int, pos_x_began : int, pos_y_began : int) {
	var com : String;
	var originScaleFactor : float = 1.0 / ifpCsInterface.csiTPTUIConfGetScale();
	if (evt == null) {
		com = "SEND_MESSAGE:/CTRL01/Touchpad/" + state + "," + 
				Mathf.Round(pos_x * originScaleFactor) + "," + Mathf.Round(pos_y * originScaleFactor) + "," + 
				Mathf.Round(pos_x_began * originScaleFactor)  + "," + Mathf.Round(pos_y_began * originScaleFactor);
		execCommand(com);
	} else {
		var n : int = evt.GetNumCommands();
		for (var i : int = 0; i < n; i++) {
			com = evt.GetCommand(i) +  state + "," + 
					Mathf.Round(pos_x * originScaleFactor) + "," + Mathf.Round(pos_y * originScaleFactor) + "," + 
					Mathf.Round(pos_x_began * originScaleFactor) + "," + Mathf.Round(pos_y_began * originScaleFactor);
			execCommand(com);
		}
	}
}

function incNetStatusCommand(evt : TPTabletUIEvent) {
	if (evt == null)
		return;
	var com : String;
	var com_str : String[];
	var com_name : String[];
	var n : int = evt.GetNumCommands();
	for (var i : int = 0; i < n; i++) {
		com = evt.GetCommand(i);
		com_str = getSplitCommandStr(com);
		com_name = getCommandName(com_str[0].ToLower());
		if (com_name[0] == "net_status")
			return true;
	}
	return false;
}

function Update () {

	updateCamera();

	if (ifpNetController.GetTPSharedInfo() == null)
		return;

	var n : int = ifpNetController.TPSInfoCheckControlEvents();
	var str : String = "";
	var evt : TPTabletUIEvent;
	if (n > 0) {
		for (var i : int = 0; i < n; i++) {
			str = ifpNetController.TPSInfoGetControlEvent(i);
			execCommand(str);
			//evt = ifpCsInterface.csiTPTUIConfFindNetworkEvent(str);
			//if (evt)
			//	doEvents(evt);
		}
		ifpNetController.TPSInfoResetControlEvent();
	}
}

function existString( targetString : String, searchingString : String ) {
	if( targetString == String.Empty ) {
		return false;
	} else {
		if( targetString.IndexOf( searchingString ) == -1 )	return false;
		else return true;
	}
}

function stringToAscii( s : String ) {
	var ascii = "";
	if( s.length > 0 ) {
		for( var i : int = 0; i < s.length; i++ ) {
			var c = "" + s[i];
			ascii += c;
 		}
 	}
 	return( ascii );
}

function SetPersonalMode( b : boolean ) {
	isPersonalMode = b;
}

function SetTransFollowMode( b : boolean ) {
	isTransFollowMode = b;
}

function setCameraTrans(str : String) {
	
	//if( isPersonalMode && !isTransFollowMode ) return;
	if( syncMode != SyncMode.VR_TO_CTRL ) return;
	
	var s : String[];
	s = str.Split( ","[0] );

	var val : float[] = new float[6];

	for( var i : int = 0; i < 6; i++ ) {
		if( !float.TryParse( s[i], val[i] ) ) {
			ifpCsInterface.csiTPTUIMngPutErrorMessage( "setCameraTrans : failed to parse value " + val[i] );
			return;
		}
	}

	//ifpCsInterface.csiTPTUIMngPutErrorMessage( "try to set CameraTrans : " + val[0] + ", " + val[1] + ", " + val[2] + ", " + val[3] + ", " + val[4] + ", " + val[5] );

	var delta : float = Time.deltaTime;
	var factor : float = delta * 5.0f;

//	Camera.main.transform.position = Vector3( -val[0], val[1], val[2] );
//	if( !isTransFollowMode )
//		Camera.main.transform.rotation = Quaternion.Euler( -val[4], 180f - val[3], -val[5] );
	Camera.main.transform.position = Vector3.Slerp( Vector3( val[0], val[1], -val[2] ), Camera.main.transform.localPosition, factor );
	//if( !isTransFollowMode )
	Camera.main.transform.rotation = Quaternion.Slerp( Quaternion.Euler( val[4], val[3], val[5] ), Camera.main.transform.localRotation, factor );

//	ifpCsInterface.csiTPTUIMngPutErrorMessage( "setCameraTrans : " + Camera.main.transform.position.x + ", " + Camera.main.transform.position.y + ", " + Camera.main.transform.position.z + ", " + 
//												Camera.main.transform.rotation.eulerAngles.x + ", " + Camera.main.transform.rotation.eulerAngles.y + ", " + Camera.main.transform.rotation.eulerAngles.z );

}

function updateCamera() {

	var pos : Vector3;
	var euler : Vector3;
	var cmd : String;

	if( syncMode == SyncMode.CTRL_TO_VR ) {
		
		pos = Camera.main.transform.position;
		euler = Camera.main.transform.eulerAngles;

		if( pos != prevPos || euler != prevEuler ) {
			cmd = "SEND_MESSAGE:/CTRL01/Command/tabletCamera_set_trans,trans=" +
					pos[0].ToString() + ":" + pos[1].ToString() + ":" + (-pos[2]).ToString() +
					",rot=" + euler[1].ToString() + ":" + euler[0].ToString() + ":" + euler[2].ToString();
			execCommand( cmd );
		}

		prevPos = pos;
		prevEuler = euler;

	} else if( syncMode == SyncMode.VR_TO_CTRL ) {
		
//		cmd = "SEND_MESSAGE:/CTRL01/Command/tabletCamera_set_trans,trans=" +
//			(-pos[0]).ToString() + ":" + pos[1].ToString() + ":" + pos[2].ToString() +
//			",rot=" + (180f - euler[1]).ToString() + ":" + (-euler[0]).ToString() + ":" + (-euler[2]).ToString();
//		execCommand( cmd );
		
	} else if( syncMode == SyncMode.INDIVIDUAL ) {

	}

}

function getControlMode() {
	return syncMode;
}

@script ExecuteInEditMode()
