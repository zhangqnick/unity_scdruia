#pragma strict

private var ifpCsInterface : IFP_CsInterface;
private var ifpEvent : IFP_Event;
private var ifpNetController : IFP_NetController;

private var m_TPTUIConfig : TPTabletUIConfig = null;

private var touchCount : int = 0;
private var maxTouchCount : int = 5;
private var touchList = new Touch[5];          	// Input.touchesを格納する配列

private var touchBtnList = new int[5];         	// touchしたときのボタンのID（-1ならボタン以外）Beganのときのみ設定
private var touchPhaseList = new TouchPhase[5]; 	// TouchPhaseの配列
private var touchPosition = new Vector2[5];    	// 現在touchしているポジション
private var touchPositionBak = new Vector2[5]; 	// 直前にtouchしていたポジション
private var touchPositionBegan = new Vector2[5];
private var onBtnList = new int[5];            	//current touch place id

private var grpOffset = Vector2(0, 0);

private var evtTypeStrings : String[] = ["touch", "release", "repeat", "loop", "move"];

private var scaleFactor : float = 1.0;

private var awaked_flag : boolean = false;
private var started_flag : boolean = false;

var customSkin : GUISkin;

private var AI : GameObject;

private	var isDebugMode : int;

public var numDebugLines : int = 25;

function Awake () {
	ifpCsInterface = GetComponent(IFP_CsInterface);
	ifpEvent = GetComponent(IFP_Event);
	ifpNetController = GetComponent(IFP_NetController);
}

function Start () {

	isDebugMode = PlayerPrefs.GetInt("debug_mode");

}

function startActIndicator() {
	//ifpNetController.sendMessage("start actIndicator");

//	Handheld.SetActivityIndicatorStyle(iOSActivityIndicatorStyle.Gray);
//	Handheld.StartActivityIndicator();
//	yield WaitForSeconds(0);

	//ifpNetController.sendMessage("end actIndicator");
}

function initTouchPrm(i : int) {
	touchBtnList[i] = -1;
	touchPhaseList[i] = TouchPhase.Canceled;
	touchPosition[i] = Vector2(-1, -1);
	touchPositionBak[i] = Vector2(-1, -1);
	touchPositionBegan[i] = Vector2(-1, -1);
	onBtnList[i] = -1;
}

function initTouchList() {
	var i : int;
	for (i = 0; i < maxTouchCount; i++) {
		initTouchPrm(i);
	}
}

function initCtrlPrms() {
	initTouchList();
	grpOffset = Vector2(0, 0);
}

function doStart () {
	initTouchList();

	m_TPTUIConfig = ifpCsInterface.csiTPTUIGetConfig();
	ifpNetController.conectNetwork();

	if (m_TPTUIConfig == null)
		return;

	Screen.orientation = ifpCsInterface.csiTPTUIConfGetOrientation();

	if (Screen.orientation == ScreenOrientation.Portrait ||
	    Screen.orientation == ScreenOrientation.PortraitUpsideDown)
		scaleFactor = Screen.height / 2048.0;
	else if (Screen.orientation == ScreenOrientation.Landscape ||
	         Screen.orientation == ScreenOrientation.LandscapeLeft ||
	         Screen.orientation == ScreenOrientation.LandscapeRight)
		scaleFactor = Screen.width / 2048.0;
	ifpCsInterface.csiTPTUIConfSetScale(scaleFactor);

	var n = ifpCsInterface.csiTPTUIConfGetNumRootGroups();

	var grp : TPTabletUIGroup;
	var idx : int;
	for (var i : int = 0; i < n; i++) {
		grp = ifpCsInterface.csiTPTUIConfGetRootGroup(i);
	}
}

//function isActiveButton(btn : TPTabletUIButton, isBegan : bool ) {
function isActiveButton(btn : TPTabletUIButton) {
	var k : int = btn.GetDataSelection();
	var defevt : boolean = isDefEvent(btn, k);
	if (!defevt)
		return -1;
	var brect : Rect = btn.GetRect();
	for (var i : int = 0; i < touchCount; i++) {
		var fid : int = touchList[i].fingerId;
//		if( isBegan ) {
			if (brect.xMin < touchPosition[fid].x && touchPosition[fid].x < brect.xMax &&
			    brect.yMin < touchPosition[fid].y && touchPosition[fid].y < brect.yMax)
			    return fid;
//		} else return fid;
	}
	return -1;
}

function isDefEvent(btn : TPTabletUIButton, i : int) {
	if (btn.GetDataEvent(i, "touch") != null ||
	    btn.GetDataEvent(i, "release") != null ||
	    btn.GetDataEvent(i, "repeat") != null ||
	    btn.GetDataEvent(i, "move") != null ) {
		return true;
	}
	return false;
}

function isValidPosition(rc : Rect, fid : int) {
	if (rc.xMin < touchPosition[fid].x && touchPosition[fid].x < rc.xMax &&
	    rc.yMin < touchPosition[fid].y && touchPosition[fid].y < rc.yMax &&
	    rc.xMin < touchPositionBegan[fid].x && touchPositionBegan[fid].x < rc.xMax &&
	    rc.yMin < touchPositionBegan[fid].y && touchPositionBegan[fid].y < rc.yMax)
		return true;

	return false;
}

function isValidPositionFromBtn(btn : TPTabletUIButton) {
	var k : int = btn.GetDataSelection();
	var defevt : boolean = isDefEvent(btn, k);
	if (!defevt)
		return false;
	var stat : boolean = false;
	var brect : Rect = btn.GetRect();
	var fid : int = -1;
	for (var i : int = 0; i < touchCount; i++) {
		fid = touchList[i].fingerId;
		stat = isValidPosition(brect, fid);
		if (stat)
		    return true;
	}
	return false;
}

private var colTable = new Array();
private var texTable = new Array();

function getColorTexture(col : Color) {
	if (col.a == 0)
		return;
	if (col.r < 0 || col.g < 0 || col.b < 0)
		return null;

	var i : int = 0;
	var n : int = colTable.length;
	for (i = 0; i < n; i++) {
		var ctcol : Color = colTable[i];
		if (ctcol.r == col.r && ctcol.g == col.g && ctcol.b == col.b)
			return texTable[i];
	}
	//print ("add color " + col);
	colTable.Push(col);
	var new_tex = new Texture2D(1, 1);
	new_tex.SetPixel(0, 0, col);
	new_tex.Apply();
	texTable.Push(new_tex);
	return new_tex;
}


function drawColoredRect(rc : Rect, col : Color) {
	if (col.a == 0)
		return;
	if (col.r < 0 || col.g < 0 || col.b < 0)
		return;

	var bg_col_tex : Texture2D = getColorTexture(col);
	if (bg_col_tex == null)
		return;

	if (customSkin)
		GUI.skin = customSkin;

//	GUI.Label(rc, bg_col_tex, "CustomButton");
	GUI.DrawTexture(rc, bg_col_tex, ScaleMode.StretchToFill, true, bg_col_tex.width/bg_col_tex.height);
}

function drawButton(btn : TPTabletUIButton) {
	if (btn == null)
		return;

	if (customSkin)
		GUI.skin = customSkin;

	var n : int = btn.GetNumData();
	var i : int = btn.GetDataSelection();
	if (i < 0 || n <= i)
		return;

	var btnId : int = btn.GetIdentifier();

	var actid : int = isActiveButton(btn);
	var astat : boolean = false;

	var bimg : Texture2D;
	bimg = btn.GetDataNormalImage(i);

	var col = new Color(-1, -1, -1, 0);
	var acol = btn.GetDataActiveBgColor(i);
	var ncol = btn.GetDataNormalBgColor(i);
	if (ncol.a > 0 && ncol.r >= 0 && ncol.g >= 0 && ncol.b >= 0)
		col = ncol;

	if (actid > -1) {
	/*** 2013/03/19 ***
		if (touchBtnList[actid] == btnId) {
			astat = true;
		}
	*** 2013/03/19 ***/
		astat = isValidPosition(btn.GetRect(), actid);
	/*** 2013/03/19 ***/
	}

	var bname : String = btn.GetName();
	if (bname != "") {
		var str : String[];
		str = bname.Split(","[0]);
		var defEvt : boolean = isDefEvent(btn, i);
		if (defEvt == false) {
			astat = false;
			//bimg = btn.GetDataNormalImage(i);
		}
		if (str[0].ToLower() == "net_status") {
			astat = ifpNetController.isConnectNetwork();
			//bimg = (ifpNetController.isConnectNetwork()) ? btn.GetDataActiveImage(i) : bimg;
		}
	}

	var evt : TPTabletUIEvent = null;
	var nstatcmd : boolean = false;
	for (var k : int = 0; k < evtTypeStrings.Length; k++) {
		//print (evtTypeStrings[k]);
		evt = btn.GetDataEvent(i, evtTypeStrings[k]);
		if (evt)
			nstatcmd = ifpEvent.incNetStatusCommand(evt);
		if (nstatcmd) {
			astat = ifpNetController.isConnectNetwork();
			//bimg = (ifpNetController.isConnectNetwork()) ? btn.GetDataActiveImage(i) : bimg;
			break;
		}
	}
/****/
	bimg = (astat) ? ((btn.GetDataActiveImage(i)) ? btn.GetDataActiveImage(i) : bimg) : bimg;
	col = (astat) ? ((acol.a > 0 && acol.r >= 0 && acol.g >= 0 && acol.b >= 0)?acol:col) : col;

	if (col.a > 0 && col.r >= 0 && col.g >= 0 && col.b >= 0)
		drawColoredRect(btn.GetRect(), col);
	if (bimg)
		//GUI.Label(btn.GetRect(), bimg, "CustomButton");
		GUI.DrawTexture(btn.GetRect(), bimg, ScaleMode.StretchToFill, true, btn.GetRect().width/btn.GetRect().height);


	var label : String = btn.GetDataTextString(i);
	var alignback : TextAnchor = GUI.skin.label.alignment;
	var fontcolorback : Color = GUI.skin.label.normal.textColor;
	var fontsizeback : int = GUI.skin.label.fontSize;
	if (label) {
		GUI.skin.customStyles[0].alignment = btn.GetDataTextAlignment(i);
		GUI.skin.customStyles[0].normal.textColor = btn.GetDataTextColor(i);
		//GUI.skin.customStyles[0].fontSize = btn.GetDataTextSize(i) * scaleFactor;
		GUI.skin.customStyles[0].fontSize = btn.GetDataTextSize(i);
		GUI.Label(btn.GetRect(), label, "CustomButton");
		GUI.skin.customStyles[0].alignment = alignback;
		GUI.skin.customStyles[0].normal.textColor = fontcolorback;
		GUI.skin.customStyles[0].fontSize = fontsizeback;
	}
}

function drawGroup(grp : TPTabletUIGroup) {
	if (grp == null)
		return;
	if (grp.GetEnable() == false)
		return;

	var gpos : Vector2 = grp.GetPosition();
	grpOffset += gpos;

	var i : int = 0;
	var nb : int = grp.GetNumButtons();
	if (nb > 0) {
		var btn : TPTabletUIButton;
		for (i = 0; i < nb; i++) {
			btn = grp.GetButton(i);
			if (btn)
				drawButton(btn);
		}
	}

	var idx : int = grp.GetIdentifier();
	var mode : TPTabletUIGroup.Mode = grp.GetMode();
	var cidx = grp.GetChildGroupSelection();
	var n : int = grp.GetNumChildGroups();
	if (n > 0) {
		var cgrp : TPTabletUIGroup;
		if (mode == TPTabletUIGroup.Mode.All) {
			for (i = 0; i < n; i++) {
				cgrp = grp.GetChildGroup(i);
				if (cgrp)
					drawGroup(cgrp);
			}
		}
		else if (-1 < cidx && cidx < n) {
			cgrp = grp.GetChildGroup(cidx);
			if (cgrp)
				drawGroup(cgrp);
		}
	}
	grpOffset -= gpos;
}

///////////////////

function parseButton(btn : TPTabletUIButton) {
	if (btn == null)
		return;

	var n : int = btn.GetNumData();
	var i : int = btn.GetDataSelection();
	if (i < 0 || n <= i)
		return;
	
	var btnId : int = btn.GetIdentifier();
	var actid : int = isActiveButton(btn);
	var astat : boolean = false;

  	var rect : Rect = btn.GetRect();
 
	var evt : TPTabletUIEvent = null;
	if (actid > -1 ) {
		onBtnList[actid] = btnId;
		if (touchPhaseList[actid] == TouchPhase.Began) {	// touch & move
			touchBtnList[actid] = btnId;
			evt = btn.GetDataEvent(i, "touch");
			if (evt)
				ifpEvent.doEvents(evt);
			
			evt = btn.GetDataEvent(i, "move");
			if (evt) { 
				ifpEvent.doMoveEvents(evt, 0, touchPosition[actid].x - rect.xMin, touchPosition[actid].y - rect.yMin,
										touchPositionBegan[actid].x - rect.xMin, touchPositionBegan[actid].y - rect.yMin);
				if( isDebugMode )
					ifpCsInterface.csiTPTUIMngPutErrorMessage(actid + " " + btn.GetName() + "[" + btnId + "] : began moving" );
			}
			
			//ifpCsInterface.csiTPTUIMngPutErrorMessage( i + " onBtn:" + onBtnList[i] + " tchBtn:" + touchBtnList[i] + " tchPhs:" + phase 
			//											+ " pos:" + touchPosition[i].x + "," + touchPosition[i].y
			//											+ " posBak:" + touchPositionBak[i].x + "," + touchPositionBak[i].y
			//											+ " posBgn:" + touchPositionBegan[i].x + "," + touchPositionBegan[i].y );

						
//			touchPhaseList[actid] = TouchPhase.Stationary;
		} else if (touchPhaseList[actid] == TouchPhase.Ended) {		// release
			/*** 2013/03/19 ***/
			astat = isValidPosition(btn.GetRect(), actid);
			if (astat) {
			/*** 2013/03/19 ***
			if (touchBtnList[actid] == btnId) {
			*** 2013/03/19 ***/
			
				evt = btn.GetDataEvent(i, "release");
				if (evt) {
					ifpEvent.doEvents(evt);
					if( isDebugMode )
						ifpCsInterface.csiTPTUIMngPutErrorMessage(actid + " " + btn.GetName() + "[" + btnId + "] : released" );
				}

				evt = btn.GetDataEvent(i, "move");
				if (evt) { 
					ifpEvent.doMoveEvents(evt, -1, touchPosition[actid].x - rect.xMin, touchPosition[actid].y - rect.yMin,
											touchPositionBegan[actid].x - rect.xMin, touchPositionBegan[actid].y - rect.yMin);
					if( isDebugMode )
						ifpCsInterface.csiTPTUIMngPutErrorMessage(actid + " " + btn.GetName() + "[" + btnId + "] : ended moving" );
				}				
				
			} else if( touchBtnList[actid] != -1 && onBtnList[actid] != touchBtnList[actid] ) {
				var btnBgn : TPTabletUIButton = null;
				btnBgn = ifpCsInterface.csiTPTUIConfFindButtonByIdentifier(touchBtnList[actid]);
				if( btnBgn ) {
					var numData : int = btnBgn.GetNumData();
					var dataSelection : int = btnBgn.GetDataSelection();
					if ( 0 <= dataSelection && dataSelection < numData ) {
						var evtBgn : TPTabletUIEvent = null;
						evtBgn = btnBgn.GetDataEvent(dataSelection, "release");
						if (evtBgn) {
							ifpEvent.doEvents(evtBgn);
							if( isDebugMode ) 
								ifpCsInterface.csiTPTUIMngPutErrorMessage( "Button:" + touchBtnList[actid] + " released (parseButton)" );
						}
						
						evtBgn = btnBgn.GetDataEvent(dataSelection, "move");
						if (evtBgn) {
							var rectBgn : Rect = btn.GetRect();
							ifpEvent.doMoveEvents(evt, -1, touchPosition[actid].x - rectBgn.xMin, touchPosition[actid].y - rectBgn.yMin,
													touchPositionBegan[actid].x - rectBgn.xMin, touchPositionBegan[actid].y - rectBgn.yMin);
							if( isDebugMode )
								ifpCsInterface.csiTPTUIMngPutErrorMessage( "Button:" + touchBtnList[actid] + " ended moving (parseButton)" );
						}
					}
				}
			}
			initTouchPrm(actid);
		} else if (touchPhaseList[actid] == TouchPhase.Moved || touchPhaseList[actid] == TouchPhase.Stationary) {	// repeat
			/*** 2013/03/19 ***/
			astat = isValidPosition(btn.GetRect(), actid);
			if (astat) {
			/*** 2013/03/19 ***
			if (touchBtnList[actid] == btnId) {
			*** 2013/03/19 ***/
				if (touchPhaseList[actid] == TouchPhase.Moved ) {		// move
					evt = btn.GetDataEvent(i, "move");
					if (evt) {
						ifpEvent.doMoveEvents(evt, 1, touchPosition[actid].x - rect.xMin, touchPosition[actid].y - rect.yMin,
												touchPositionBegan[actid].x - rect.xMin, touchPositionBegan[actid].y - rect.yMin);
						if( isDebugMode )
							ifpCsInterface.csiTPTUIMngPutErrorMessage(actid + " " + btn.GetName() + "[" + btnId + "] : moving" );
					}
				}
				
				evt = btn.GetDataEvent(i, "repeat");
				if (evt)
					ifpEvent.doEvents(evt); 
			}
		} else if (touchPhaseList[actid] == TouchPhase.Canceled) {
			initTouchPrm(actid);
		}
	}
	evt = btn.GetDataEvent(i, "loop");
	if (evt)
		ifpEvent.doEvents(evt);
}

function parseGroup_tch(grp : TPTabletUIGroup) {
	if (grp == null)
		return;
	if (grp.GetEnable() == false)
		return;

	var gpos : Vector2 = grp.GetPosition();
	grpOffset += gpos;

	var i : int = 0;
	var nb : int = grp.GetNumButtons();
	if (nb > 0) {
		var btn : TPTabletUIButton;
		for (i = 0; i < nb; i++) {
			btn = grp.GetButton(i);
			if (btn)
				parseButton(btn);
		}
	}

	var idx : int = grp.GetIdentifier();
	var mode : TPTabletUIGroup.Mode = grp.GetMode();
	var cidx = grp.GetChildGroupSelection();
	var n : int = grp.GetNumChildGroups();

	if (n > 0) {
		var cgrp : TPTabletUIGroup;
		if (mode == TPTabletUIGroup.Mode.All) {
			for (i = 0; i < n; i++) {
				cgrp = grp.GetChildGroup(i);
				if (cgrp)
					parseGroup_tch(cgrp);
			}
		}
		else if (-1 < cidx && cidx < n) {
			cgrp = grp.GetChildGroup(cidx);
			if (cgrp)
				parseGroup_tch(cgrp);
		}
	}
	grpOffset -= gpos;
}

///////////////////

function drawErrorMessage() {
	if( isDebugMode == 0 ) 
		return;

	var msStr : String = "+++ Failed to initialize the application. +++\n\n";
	var n : int = ifpCsInterface.csiTPTUIMngGetNumErrorMessages();
	if (n <= 0)
		return;
	msStr += "\n";
	
	var i : int = 0;
	if( n > numDebugLines )
		i = n - numDebugLines;
		
	for ( ; i < n; i++) {
		msStr += ("[" + (i+1) + "] ");
		msStr += ifpCsInterface.csiTPTUIMngGetErrorMessage(i);
		msStr += "\n";
	}

	var fontsizeback : int = GUI.skin.label.fontSize;
	var alignback : TextAnchor = GUI.skin.label.alignment;
//	GUI.skin.label.fontSize = (Screen.width/60.0) * scaleFactor;
//	GUI.skin.label.alignment = TextAnchor.MiddleLeft;
	GUI.Label(Rect(Screen.width/2.0-Screen.width*0.4, 3*scaleFactor, Screen.width*0.8, Screen.height-3*scaleFactor), msStr);
//	GUI.skin.label.fontSize = fontsizeback;
//	GUI.skin.label.alignment = alignback;

	fontsizeback = GUI.skin.button.fontSize;
	//GUI.skin.button.fontSize = 40 * scaleFactor;
	if (GUI.Button(Rect(Screen.width/2.0-100*scaleFactor, Screen.height-160*scaleFactor, 200*scaleFactor, 80*scaleFactor), "EXIT")) {
		ifpEvent.OnDisable();
	}
//	GUI.skin.button.fontSize = fontsizeback;
}

///////////////////

function OnGUI () {
	if (awaked_flag == false)
		return;

	var grp : TPTabletUIGroup;
	var n : int = ifpCsInterface.csiTPTUIConfGetNumRootGroups();

	var numError : int = ifpCsInterface.csiTPTUIMngGetNumErrorMessages();

	for (var i : int = 0; i < n; i++) {
		grp = ifpCsInterface.csiTPTUIConfGetRootGroup(i);
		if (grp)
			drawGroup(grp);
	}

	if (m_TPTUIConfig == null || numError > 0) {
		drawErrorMessage();
	}

}

function mouseInput() {
	var fid : int = 0; 	// finger ID これでtouch関連の配列にアクセスする

	if (Input.GetMouseButtonDown(0)) {
		touchCount = 1;
		fid = 0;
		touchPosition[fid].x = touchPositionBak[fid].x = touchPositionBegan[fid].x = Input.mousePosition.x;
		touchPosition[fid].y = touchPositionBak[fid].y = touchPositionBegan[fid].y = Screen.height - Input.mousePosition.y;
		touchPhaseList[fid] = TouchPhase.Began;
	} else if (Input.GetMouseButtonUp(0)) {
		touchCount = 1;
		touchPositionBak[fid] = touchPosition[fid];
		touchPosition[fid].x = Input.mousePosition.x;
		touchPosition[fid].y = Screen.height - Input.mousePosition.y;
		touchPhaseList[fid] = TouchPhase.Ended;
	} else if (Input.GetMouseButton(0)) {
		touchCount = 1;
		touchPositionBak[fid] = touchPosition[fid];
		touchPosition[fid].x = Input.mousePosition.x;
		touchPosition[fid].y = Screen.height - Input.mousePosition.y;
		touchPhaseList[fid] = TouchPhase.Moved;
	}
}

function Update () {
	if (awaked_flag == false) {
		var procStat : TPTabletUIManager.InitProcStat = ifpCsInterface.csiTPTUIGetInitProcStat();
		if (procStat == TPTabletUIManager.InitProcStat.InProgress) {
			return;
		} else if (procStat == TPTabletUIManager.InitProcStat.Success) {
			awaked_flag = true;
			doStart();
			started_flag = true;
		} else if (procStat == TPTabletUIManager.InitProcStat.Fail) {
			awaked_flag = true;
		}
	}

	if (awaked_flag && started_flag) {
		var fid : int = 0; 	// finger ID これでtouch関連の配列にアクセスする

		touchCount = 0;
		for (var touch : Touch in Input.touches) {
			touchList[touchCount] = touch;
			fid = touch.fingerId;
			if (fid < touchList.Length) {
				onBtnList[fid] = -1;
				if (touch.phase == TouchPhase.Began) {
					touchPosition[fid].x = touchPositionBak[fid].x = touchPositionBegan[fid].x = touch.position.x;
					touchPosition[fid].y = touchPositionBak[fid].y = touchPositionBegan[fid].y = Screen.height - touch.position.y;
					touchPhaseList[fid] = TouchPhase.Began;
				} else if (touch.phase == TouchPhase.Ended) {
					touchPositionBak[fid] = touchPosition[fid];
					touchPosition[fid].x = touch.position.x;
					touchPosition[fid].y = Screen.height - touch.position.y;
					touchPhaseList[fid] = TouchPhase.Ended;
				} else if (touch.phase == TouchPhase.Moved) {
					touchPositionBak[fid] = touchPosition[fid];
					touchPosition[fid].x = touch.position.x;
					touchPosition[fid].y = Screen.height - touch.position.y;
					touchPhaseList[fid] = TouchPhase.Moved;
				} else if (touch.phase == TouchPhase.Stationary) {
					touchPositionBak[fid] = touchPosition[fid];
					touchPosition[fid].x = touch.position.x;
					touchPosition[fid].y = Screen.height - touch.position.y;
					touchPhaseList[fid] = TouchPhase.Stationary;
				} else if (touch.phase == TouchPhase.Canceled) {
					touchPositionBak[fid] = touchPosition[fid];
					touchPosition[fid].x = touch.position.x;
					touchPosition[fid].y = Screen.height - touch.position.y;
					touchPhaseList[fid] = TouchPhase.Canceled;
				}
			}	//  if (fid < touchBtnList.Length)
			
			touchCount++;
			if (maxTouchCount <= touchCount)
				break;
		}
	
		if (touchCount == 0)
			mouseInput();

		isDebugMode = PlayerPrefs.GetInt("debug_mode");

		var grp : TPTabletUIGroup;
		var n : int = ifpCsInterface.csiTPTUIConfGetNumRootGroups();
		for (var i : int = 0; i < n; i++) {
			grp = ifpCsInterface.csiTPTUIConfGetRootGroup(i);
			if (grp)
				parseGroup_tch(grp);
		}
	
		for (i = 0; i < maxTouchCount; i++) {
		
			if( isDebugMode ) {
				if( touchPhaseList[i] == TouchPhase.Ended || touchPhaseList[i] == TouchPhase.Began || touchPhaseList[i] == TouchPhase.Moved
					|| touchPhaseList[i] == TouchPhase.Stationary ) {
					var phase : String;
					if( touchPhaseList[i] == TouchPhase.Ended ) phase = "Ended";
					else if( touchPhaseList[i] == TouchPhase.Began ) phase = "Began";
					else if( touchPhaseList[i] == TouchPhase.Moved ) phase = "Moved";
					else if( touchPhaseList[i] == TouchPhase.Stationary ) phase = "Stationary";
					else if( touchPhaseList[i] == TouchPhase.Canceled ) phase = "Canceled";

					ifpCsInterface.csiTPTUIMngPutErrorMessage( i + " onBtn:" + onBtnList[i] + " tchBtn:" + touchBtnList[i] + " tchPhs:" + phase 
																+ " pos:" + touchPosition[i].x + "," + touchPosition[i].y
																+ " posBak:" + touchPositionBak[i].x + "," + touchPositionBak[i].y
																+ " posBgn:" + touchPositionBegan[i].x + "," + touchPositionBegan[i].y );
				}
			}
			
			if (touchPhaseList[i] == TouchPhase.Ended || touchPhaseList[i] == TouchPhase.Canceled) {
				if (onBtnList[i] == -1 || onBtnList[i] != touchBtnList[i] ) {
					if (touchBtnList[i] != -1) {				
						if (grp) {
							var btn : TPTabletUIButton = null;
							btn = ifpCsInterface.csiTPTUIConfFindButtonByIdentifier(touchBtnList[i]);
							
							if( btn ) {
								var numData : int = btn.GetNumData();
								var dataSelection : int = btn.GetDataSelection();
								if ( 0 <= dataSelection && dataSelection < numData ) {
									var evt : TPTabletUIEvent = null;
									evt = btn.GetDataEvent(dataSelection, "release");
									if (evt) {
										ifpEvent.doEvents(evt);
										if( isDebugMode ) {
											ifpCsInterface.csiTPTUIMngPutErrorMessage( "Button:" + touchBtnList[i] + " released (update)" );
										}
									}
									
									evt = btn.GetDataEvent(dataSelection, "move");
									if (evt) {
										var rect : Rect = btn.GetRect();
										ifpEvent.doMoveEvents(evt, -1, touchPosition[i].x - rect.xMin, touchPosition[i].y - rect.yMin,
																touchPositionBegan[i].x - rect.xMin, touchPositionBegan[i].y - rect.yMin);
										if( isDebugMode )
											ifpCsInterface.csiTPTUIMngPutErrorMessage( "Button:" + touchBtnList[i] + " ended moving (update)" );
									}
								}
							}
						}
					}
					initTouchPrm(i);
				}
			}
		}
	}
}

@script ExecuteInEditMode()
