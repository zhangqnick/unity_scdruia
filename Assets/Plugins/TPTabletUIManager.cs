using System;
using System.Text;
using System.IO;
using System.Collections;
using UnityEngine;

public class TPTabletUIManager : MonoBehaviour {
	/** The filename of the XML configuration file. */
	private const string	m_filenameXML = "TabletInterface.xml";
	/** The filename of the asset bundle. */
	private const string	m_filenameAssetBundle = "TabletInterface.unity3d";
	
	/** The instance of the GUI configuration class. */
	private TPTabletUIConfig	m_config = null;
	/** The mapping between the image (file) names and the Texture2D instances. */
	private Hashtable	m_images = new Hashtable();
	/** The error messages. */
	private ArrayList	m_errMsgs = new ArrayList();
	
	/** The enumeration values which describe the status of the initialization procedure. */
	public enum InitProcStat { None, InProgress, Fail, Success };
	/** The current status of the initialization procedure. */
	private InitProcStat	m_initProcStat = InitProcStat.None;
	
	/** The enumeration values which describe the status of loading the asset bundle. */
	private enum AssetBundleLoadStat { None, InProgress, Fail, Success };
	/** The current status of loading the asset bundle. */
	private AssetBundleLoadStat	m_assetBundleLoadStat = AssetBundleLoadStat.None;
	
	/** Gets the only one TPTabletUIManager class instance. */
	public static TPTabletUIManager GetInstance() {
		// Get the reference to the TPTabletUIManager script component.
		GameObject go = GameObject.Find("TPTabletUIManager");
		if(go == null){
			Debug.LogError("The TPTabletUIManager GameObject object is not found.");
			return null;
		}
		TPTabletUIManager inst = go.GetComponent(typeof(TPTabletUIManager)) as TPTabletUIManager;
		if(inst == null)
			Debug.LogError("The TPTabletUIManager script component is not found.");
		return inst;
	}
	
	/** Gets the current status of the initialization procedure. */
	public InitProcStat GetInitProcStat() { return m_initProcStat; }
	
	/** Gets the instance of the GUI configuration class. */
	public TPTabletUIConfig GetConfig() { return m_config; }
	
	private Texture2D LoadImage(string file) {
		string path = Path.Combine(Application.persistentDataPath, file);
		string ext = Path.GetExtension(path);
		if(string.IsNullOrEmpty(ext)) path += ".png";
		byte[] bytes;
		try {
			bytes = File.ReadAllBytes(path);
		}catch(Exception e){
			PutErrorMessage("Failed to read " + file + ".png from the cache directory.");
			PutErrorMessage(e.Message);
			return null;
		}
		Texture2D tex = new Texture2D(8, 8, TextureFormat.ARGB32, false);
		if(tex.LoadImage(bytes)){
			tex.Apply(false, false);
			return tex;
		}
		PutErrorMessage("Failed to load pixels into the texture. (" + file + ".png)");
		return null;
	}
	
	private Texture2D FindLoadedImage(string name) {
		return ((m_images != null && m_images.ContainsKey(name)) ? m_images[name] : null) as Texture2D;
	}
	
	/** Gets the Texture2D instance which has the specified name. */
	public Texture2D GetImage(string name) {
		Texture2D image = FindLoadedImage(name);
		if(image == null){
			// Load the image data from the cache, and create & insert a Texture2D instance to the hashtable.
			image = LoadImage(name);
			if(image != null)
				m_images[name] = image;
		}
		return image;
	}
	
	private void DeleteFile(string fname) {
		string path = Path.Combine(Application.persistentDataPath, fname);
		if(File.Exists(path)){
			try {
				File.Delete(path);
			}catch(Exception e){
				PutErrorMessage("Failed to delete " + fname + " from the cache directory.");
				PutErrorMessage(e.Message);
			}
		}
	}
	
	private bool DownloadFiles() {
		PutErrorMessage("Try to download asset files.");
		TPNetworkClient nc = new TPNetworkClient();
		if(!nc.Initialize()){
			PutErrorMessage("Failed to initialize the network client.");
			return false;
		}
		
		DeleteFile(m_filenameAssetBundle);
		
		bool result = true;
		
		// Get the names of all the GUI data files (including the asset bundle).
		string[] files = nc.RequestGUIFileList();
		if(files == null)
			result = false;
		else{
			// Download all the GUI data files to the cache directory.
			int i;
			for(i=0; i<files.Length; i++){
				byte[] bytes = nc.RequestGUIFile(files[i]);
				if(bytes == null){
					result = false;
					break;
				}
				string path = Path.Combine(Application.persistentDataPath, files[i]);
				PutErrorMessage("Try to write doownloaded files : " + path );
				try {
					File.WriteAllBytes(path, bytes);
				}catch(Exception e){
					PutErrorMessage(e.Message);
					PutErrorMessage("Failed to save the downloaded file : " + files[i]);
					result = false;
					break;
				}
			}
		}
		
		nc.Terminate();
		return result;
	}
	
	/** The coroutine for loading the asset bundle from the application data directory. */
	IEnumerator LoadAssetBundle()
	{
		string path = Path.Combine(Application.persistentDataPath, m_filenameAssetBundle);
		if(!File.Exists(path)){
			m_assetBundleLoadStat = AssetBundleLoadStat.Success;
			yield break;
		}
		string url = "file://" + path;
		Debug.Log("[INFO] Issuing a file request : " + url);
		WWW www = new WWW(url);

		// Wait for completion of the request.
		while(true){
			if(string.IsNullOrEmpty(www.error)){
				if(www.isDone)
					break;
			}else{
				m_assetBundleLoadStat = AssetBundleLoadStat.Fail;
				PutErrorMessage("Failed to load the asset bundle : " + www.error);
				yield break;
			}
			yield return null;
		}
		
		// NOTE:
		// On iOS, WWW doesn't seem to give us any error message when some error occurs.
		if(www.assetBundle == null){
			PutErrorMessage("Failed to load the asset bundle (no error messages)");
			m_assetBundleLoadStat = AssetBundleLoadStat.Fail;
			yield break;
		}
		
		// Extract all the image data from the asset bundle.
		UnityEngine.Object[] images = www.assetBundle.LoadAllAssets(typeof(Texture2D));
		int i;
		for(i=0; i<images.Length; i++){
			Texture2D tex = images[i] as Texture2D;
			Debug.Log("Loaded from the asset bundle : " + tex.name);
			m_images[tex.name] = tex;
		}
		
		// Unload all the unnecessary (unused) objects.
		www.assetBundle.Unload(false);
		
		m_assetBundleLoadStat = AssetBundleLoadStat.Success;
	}
	
	/** Puts an error message. */
	public void PutErrorMessage(string errMsg) {
		m_errMsgs.Add(errMsg);
		if( m_errMsgs.Count > 99 ) {
			m_errMsgs.RemoveRange( 0, m_errMsgs.Count - 99 );
		}
	}
	/** Gets the number of the error messages. */
	public int GetNumErrorMessages() {
		return m_errMsgs.Count;
	}
	/** Gets the i-th error message. */
	public string GetErrorMessage(int i) {
		return m_errMsgs[i] as string;
	}
	/** Gets the last error message. */
	public string GetLastErrorMessage() {
		int n = GetNumErrorMessages();
		return GetErrorMessage(n - 1);
	}
	
	void Awake() {

		bool isChangePrefs = false;
		if (!PlayerPrefs.HasKey ("ip_address")) {
			PlayerPrefs.SetString ("ip_address", "10.120.64.108");
			isChangePrefs = true;
		}
		if (!PlayerPrefs.HasKey ("port_number")) {
			PlayerPrefs.SetString ("port_number", "28000");
			isChangePrefs = true;
		}
		if (!PlayerPrefs.HasKey ("clear_cache")) {
			PlayerPrefs.SetInt ("clear_cache", 1);
			isChangePrefs = true;
		}
		if (!PlayerPrefs.HasKey ("debug_mode")) {
			PlayerPrefs.SetInt ("debug_mode", 0);
			isChangePrefs = true;
		}
		if (!PlayerPrefs.HasKey ("auto_connect")) {
			PlayerPrefs.SetInt ("auto_connect", 1);
			isChangePrefs = true;
		}


		if (isChangePrefs) {
			PlayerPrefs.Save ();
			Debug.Log("PlayerPrefs updated");
		}

		// Indicate that the initialization procedure has been started.
		m_initProcStat = InitProcStat.InProgress;
		int isClearCache = PlayerPrefs.GetInt("clear_cache");

		PutErrorMessage("[Clear Cache] configuration was set [" + isClearCache.ToString() + "].");
		Debug.Log("[Clear Cache] configuration was set [" + isClearCache.ToString() + "].");
		//PutErrorMessage("[Clear Cache] configuration was set [" + PlayerPrefs.GetString("clear_cache") + "].");

		// Check if we need to download the GUI data.
		//if(PlayerPrefs.GetString("clear_cache", "0") == "1"){
		//if(PlayerPrefs.GetInt("clear_cache") == 1){
		if(isClearCache == 1){
		//if(PlayerPrefs.GetString("clear_cache") == "1"){
			if(!DownloadFiles()){
				m_initProcStat = InitProcStat.Fail;	// The initialization process has failed.
				return;
			}
			// We succeeded in downloading the GUI data, so let us disable the switch.
			//PlayerPrefs.SetString("clear_cache", "0");
			//PlayerPrefs.Save();
		}

		//Application.targetFrameRate = 60;
	}
	
	void Update() {
		if(m_initProcStat != InitProcStat.InProgress) return;
		if(m_assetBundleLoadStat == AssetBundleLoadStat.None){
			// Start loading the asset bundle.
			m_assetBundleLoadStat = AssetBundleLoadStat.InProgress;
			StartCoroutine(LoadAssetBundle());
		}else if(m_assetBundleLoadStat == AssetBundleLoadStat.Success){
			// We've succeeded in loading the asset bundle.
			// Load the XML configuration file from the cache directory, and create the TPTabletUIConfig instance.
			string path = Path.Combine(Application.persistentDataPath, m_filenameXML);
			byte[] bytes;
			try {
				bytes = File.ReadAllBytes(path);
			}catch(Exception e){
				PutErrorMessage("TPTabletUIManager::Update");
				PutErrorMessage(e.Message);
				m_initProcStat = InitProcStat.Fail;
				return;
			}
			m_config = new TPTabletUIConfig();
			if(!m_config.Parse(bytes)){	// All the GUI data files are loaded during parsing.
				m_config = null;
				m_initProcStat = InitProcStat.Fail;
				return;
			}
			m_initProcStat = InitProcStat.Success;	// The initialization procedure has successfully ended.
		}else if(m_assetBundleLoadStat == AssetBundleLoadStat.Fail){
			// We've failed in loadind the asset bundle.
			m_initProcStat = InitProcStat.Fail;
			return;
		}
	}
}
