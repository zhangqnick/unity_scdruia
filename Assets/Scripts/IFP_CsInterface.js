#pragma strict

/****/
private var TPTUIM : GameObject;
private var m_TPTUIManager : TPTabletUIManager = null;
/****/

private var m_TPTUIConfig : TPTabletUIConfig = null;

function Awake () {
	TPTUIM = GameObject.Find("TPTabletUIManager");
	m_TPTUIManager = TPTUIM.GetComponent(TPTabletUIManager);
}

function Start () {

}

function csiTPTUIGetInitProcStat() {
	var procStat : TPTabletUIManager.InitProcStat;
	if (m_TPTUIManager == null) {
		procStat = TPTabletUIManager.InitProcStat.Fail;
	} else {
		procStat = m_TPTUIManager.GetInitProcStat();
	}
	return procStat;
}

function csiTPTUIGetConfig() {
/************** 20130416
	m_TPTUIManager = TPTabletUIManager.GetInstance();
20130416 **************/

	if (m_TPTUIManager == null)
		m_TPTUIConfig = null;
	else
		m_TPTUIConfig = m_TPTUIManager.GetConfig();
/****/
	return m_TPTUIConfig;
}

function csiTPTUIResetPrms() {
	if (m_TPTUIConfig == null)
		return;
	m_TPTUIConfig.ResetAllAttributes();
}

// /////////// TPTabletUIConfig // /////////// 

function csiTPTUIConfFindNetworkEvent(type : String) {
	if (m_TPTUIConfig == null)
		return null;
	return m_TPTUIConfig.FindNetworkEvent(type);
}

function csiTPTUIConfFindGroupByName(name : String) {
	if (m_TPTUIConfig == null)
		return null;
	return m_TPTUIConfig.FindGroupByName(name);
}

function csiTPTUIConfFindGroupByIdentifier(identifier : int) {
	if (m_TPTUIConfig == null)
		return null;
	return m_TPTUIConfig.FindGroupByIdentifier(identifier);
}

function csiTPTUIConfFindChildGroupIdByName(grp : TPTabletUIGroup, name : String) {
	if (m_TPTUIConfig == null)
		return -1;
	return m_TPTUIConfig.FindChildGroupIdByName(grp, name);
}

function csiTPTUIConfFindButtonByName(name : String) {
	if (m_TPTUIConfig == null)
		return null;
	return m_TPTUIConfig.FindButtonByName(name);
}

function csiTPTUIConfFindButtonByIdentifier(identifier : int) {
	if (m_TPTUIConfig == null)
		return null;
	return m_TPTUIConfig.FindButtonByIdentifier(identifier);
}

function csiTPTUIConfGetOrientation() {
	if (m_TPTUIConfig == null)
		return ScreenOrientation.LandscapeLeft;
	return m_TPTUIConfig.GetOrientation();
}

function csiTPTUIConfSetScale(scale : float) {
	if (m_TPTUIConfig == null)
		return;
	m_TPTUIConfig.SetScale(scale);
}

function csiTPTUIConfGetScale() {
	if (m_TPTUIConfig == null)
		return 1.0;
	return m_TPTUIConfig.GetScale();
}

function csiTPTUIConfGetRect() {
	if (m_TPTUIConfig == null)
		return Rect(0, 0, 0, 0);
	return m_TPTUIConfig.GetRect();
}

function csiTPTUIConfGetNumNetworkEvents() {
	if (m_TPTUIConfig == null)
		return 0;
	return m_TPTUIConfig.GetNumNetworkEvents();
}

function csiTPTUIConfGetNetworkEvent(i : int) {
	if (m_TPTUIConfig == null)
		return null;
	return m_TPTUIConfig.GetNetworkEvent(i);
}

function csiTPTUIConfGetNumRootGroups() {
	if (m_TPTUIConfig == null)
		return 0;
	return m_TPTUIConfig.GetNumRootGroups();
}

function csiTPTUIConfGetRootGroup(i : int) {
	if (m_TPTUIConfig == null)
		return null;
	return m_TPTUIConfig.GetRootGroup(i);
}

function csiTPTUIMngGetNumErrorMessages() {
	if (m_TPTUIManager == null)
		return 0;
	return m_TPTUIManager.GetNumErrorMessages();
}

function csiTPTUIMngGetErrorMessage(i : int) {
	if (m_TPTUIManager == null)
		return null;
	return m_TPTUIManager.GetErrorMessage(i);
}

function csiTPTUIMngGetLastErrorMessage() {
	if (m_TPTUIManager == null)
		return null;
	return m_TPTUIManager.GetLastErrorMessage();
}

function csiTPTUIMngPutErrorMessage(errorStr : String) {
	if (m_TPTUIManager == null)
		return null;
	m_TPTUIManager.PutErrorMessage(errorStr);
	return true;
}

//function csiTPTUIMngConvertUnicodeToAscii(unicodeString : String) {
//	if (m_TPTUIManager == null)
//		return null;
//	return m_TPTUIManager.ConvertUnicodeToAscii(unicodeString);
//}

///////////////////////////////////

function Update () {

}

@script ExecuteInEditMode()
