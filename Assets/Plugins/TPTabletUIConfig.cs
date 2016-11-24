using UnityEngine;
using System.Collections;

public class TPTabletUIEvent {
	private string	m_type;
	private ArrayList	m_commands = new ArrayList();
	
	public bool Parse(ConfigElement ce) {
		TPTabletUIManager mgr = TPTabletUIManager.GetInstance();
		
		m_type = ce.GetAttribute("type");
		if(string.IsNullOrEmpty(m_type)){
			mgr.PutErrorMessage("Found an Event/NetworkEvent element which doesn't have the type attribute.");
			return false;
		}
		
		int i;
		for(i=0; i<ce.GetNumChildren(); i++){
			ConfigElement child = ce.GetChild(i);
			if(child.GetName() == "Action"){
				string command = child.GetAttribute("command");
				if(!string.IsNullOrEmpty(command))
					m_commands.Add(command);
			}
		}
		return true;
	}
	
	//////////////////////////////////////////////////////////////////////////
	// The external interface methods for the GUI scripts.
	//

	/** Gets the type of the event. */
	public string GetEventType() {
		return m_type;
	}
	
	/** Gets the number of the commands. */
	public int GetNumCommands() {
		return m_commands.Count;
	}
	/** Gets the i-th command. */
	public string GetCommand(int i) {
		return m_commands[i] as string;
	}
}

public class TPTabletUIButton {
	private int	m_identifier = -1;
	private string	m_name;
	private Vector2	m_position = new Vector2(-1.0f, -1.0f);	// Relative position to the parent element.
	private Vector2	m_size = new Vector2(-1.0f, -1.0f);
	private int	m_selection = -1;
	private int	m_orgSelection = -1;
	private ArrayList	m_data = new ArrayList();
	
	/** The absolute position and size (the bounding rectangle). */
	private Rect	m_boundRect = new Rect(0.0f, 0.0f, 0.0f, 0.0f);
	
	public TPTabletUIButton(int identifier) {
		m_identifier = identifier;
	}
	
	public class Data {
		private TPTabletUIButton	m_parent = null;
		private string	m_textString;
		private Color	m_textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		private int	m_textSize = 12;
		private TextAnchor	m_textAlignment = TextAnchor.MiddleCenter;
		private Texture2D[]	m_images = { null, null };	/**< 0:normal, 1:active. */
		private Color[]	m_bgColors = {
			new Color(0.0f, 0.0f, 0.0f, 0.0f),
			new Color(0.0f, 0.0f, 0.0f, 0.0f)
		};
		private	Hashtable	m_events = new Hashtable();
		
		public Data(TPTabletUIButton parent) {
			m_parent = parent;
		}
		
		public bool Parse(ConfigElement ce) {
			TPTabletUIManager mgr = TPTabletUIManager.GetInstance();
			m_textString = ce.GetAttribute("textString");
			string attrib = ce.GetAttribute("textColor");
			if(!string.IsNullOrEmpty(attrib)){
				string[] vec = attrib.Split(' ');
				if(vec.Length != 3){
					mgr.PutErrorMessage("Found an invalid textColor attribute (Button/Data : "
						+ m_parent.GetName() + ").");
					return false;
				}
				float r, g, b;
				float.TryParse(vec[0], out r);
				float.TryParse(vec[1], out g);
				float.TryParse(vec[2], out b);
				m_textColor.r = r / 255.0f;
				m_textColor.g = g / 255.0f;
				m_textColor.b = b / 255.0f;
			}
			attrib = ce.GetAttribute("textSize");
			if(!string.IsNullOrEmpty(attrib))
				int.TryParse(attrib, out m_textSize);
			attrib = ce.GetAttribute("textAlignment");
			if(!string.IsNullOrEmpty(attrib)){
				if(attrib == "left" || attrib == "middle_left")
					m_textAlignment = TextAnchor.MiddleLeft;
				else if(attrib == "celter" || attrib == "middle_center")
					m_textAlignment = TextAnchor.MiddleCenter;
				else if(attrib == "right" || attrib == "middle_right")
					m_textAlignment = TextAnchor.MiddleRight;
				else if(attrib == "upper_left") m_textAlignment = TextAnchor.UpperLeft;
				else if(attrib == "upper_center") m_textAlignment = TextAnchor.UpperCenter;
				else if(attrib == "upper_right") m_textAlignment = TextAnchor.UpperRight;
				else if(attrib == "lower_left") m_textAlignment = TextAnchor.LowerLeft;
				else if(attrib == "lower_center") m_textAlignment = TextAnchor.LowerCenter;
				else if(attrib == "lower_right") m_textAlignment = TextAnchor.LowerRight;
				else{
					mgr.PutErrorMessage("Found an invalid textAlignment attribute (Button/Data : "
						+ m_parent.GetName() + ") : " + attrib);
					return false;
				}
			}
			attrib = ce.GetAttribute("normalImage");
			if(!string.IsNullOrEmpty(attrib)){
				m_images[0] = mgr.GetImage(attrib);
				if(m_images[0] == null){
					mgr.PutErrorMessage("No such an image : " + attrib);
					return false;
				}
			}
			attrib = ce.GetAttribute("activeImage");
			if(!string.IsNullOrEmpty(attrib)){
				m_images[1] = mgr.GetImage(attrib);
				if(m_images[1] == null){
					mgr.PutErrorMessage("No such an image : " + attrib);
					return false;
				}
			}
			attrib = ce.GetAttribute("normalBgColor");
			if(!string.IsNullOrEmpty(attrib)){
				string[] vec = attrib.Split(' ');
				if(vec.Length != 3){
					mgr.PutErrorMessage("Found an invalid normalBgColor attribute (Button/Data : "
						+ m_parent.GetName() + ").");
					return false;
				}
				float r, g, b;
				float.TryParse(vec[0], out r);
				float.TryParse(vec[1], out g);
				float.TryParse(vec[2], out b);
				m_bgColors[0].r = r / 255.0f;
				m_bgColors[0].g = g / 255.0f;
				m_bgColors[0].b = b / 255.0f;
				m_bgColors[0].a = 1.0f;	// Indicates that the normalBgColor attribute has been specified.
			}
			attrib = ce.GetAttribute("activeBgColor");
			if(!string.IsNullOrEmpty(attrib)){
				string[] vec = attrib.Split(' ');
				if(vec.Length != 3){
					mgr.PutErrorMessage("Found an invalid activeBgColor attribute (Button/Data : "
						+ m_parent.GetName() + ").");
					return false;
				}
				float r, g, b;
				float.TryParse(vec[0], out r);
				float.TryParse(vec[1], out g);
				float.TryParse(vec[2], out b);
				m_bgColors[1].r = r / 255.0f;
				m_bgColors[1].g = g / 255.0f;
				m_bgColors[1].b = b / 255.0f;
				m_bgColors[1].a = 1.0f;	// Indicates that the activeBgColor attribute has been specified.
			}
			
			int i;
			for(i=0; i<ce.GetNumChildren(); i++){
				ConfigElement child = ce.GetChild(i);
				string name = child.GetName();
				if(name == "Event"){
					TPTabletUIEvent buttonEvent = new TPTabletUIEvent();
					if(buttonEvent.Parse(child))
						m_events[buttonEvent.GetEventType()] = buttonEvent;
					else return false;
				}
			}
			
			return true;
		}
		
		public void SetTextString(string textString) {
			m_textString = textString;
		}
		public string GetTextString() {
			return m_textString;
		}
		public Color GetTextColor() {
			return m_textColor;
		}
		public int GetTextSize() {
			return m_textSize;
		}
		public TextAnchor GetTextAlignment() {
			return m_textAlignment;
		}
		public Texture2D GetTextImage(int i) {
			return m_images[i];
		}
		public Color GetBgColor(int i) {
			return m_bgColors[i];
		}
		public TPTabletUIEvent GetEvent(string type) {
			return (m_events.ContainsKey(type) ? m_events[type] : null) as TPTabletUIEvent;
		}
	}
	
	public bool Parse(ConfigElement ce) {
		TPTabletUIManager mgr = TPTabletUIManager.GetInstance();
		
		m_name = ce.GetAttribute("name");
		
		string attrib = ce.GetAttribute("position");
		if(string.IsNullOrEmpty(attrib)){
			mgr.PutErrorMessage("Found a Button element (" 
				+ GetName() + ") which doesn't have the position attribute.");
			return false;
		}else{
			string[] vec = attrib.Split(' ');
			if(vec.Length != 2){
				mgr.PutErrorMessage("Found an invalid position attribute (Button : "
					+ GetName() + ").");
				return false;
			}
			float.TryParse(vec[0], out m_position.x);
			float.TryParse(vec[1], out m_position.y);
		}
		
		attrib = ce.GetAttribute("size");
		if(string.IsNullOrEmpty(attrib)){
			mgr.PutErrorMessage("Found a Button element ("
				+ GetName() + ") which doesn't have the size attribute.");
			return false;
		}else{
			string[] vec = attrib.Split(' ');
			if(vec.Length != 2){
				mgr.PutErrorMessage("Found an invalid size attribute (Button : "
					+ GetName() + ").");
				return false;
			}
			float.TryParse(vec[0], out m_size.x);
			float.TryParse(vec[1], out m_size.y);
		}
	
		int i;
		for(i=0; i<ce.GetNumChildren(); i++){
			ConfigElement child = ce.GetChild(i);
			string name = child.GetName();
			if(name == "Selection"){
				attrib = child.GetAttribute("index");
				if(!string.IsNullOrEmpty(attrib)){
					int.TryParse(attrib, out m_selection);
					m_orgSelection = m_selection;
				}
			}else if(name == "Data"){
				Data datum = new Data(this);
				if(datum.Parse(child))
					m_data.Add(datum);
				else return false;
			}
		}
		
		return true;
	}
	
	/** Calculates the bounding rectangle. */
	public void CalculateRect(Vector2 p, ref Vector2 maxCoords) {
		m_boundRect = new Rect(p.x + m_position.x, p.y + m_position.y, m_size.x, m_size.y);
		float rbx = m_boundRect.x + m_boundRect.width, rby = m_boundRect.y + m_boundRect.height;
		if(rbx > maxCoords.x) maxCoords.x = rbx;
		if(rby > maxCoords.y) maxCoords.y = rby;
	}
	
	/** Resets all the "settable" attributes to their default values. */
	public void ResetAllAttributes() {
		SetDataSelection(m_orgSelection);
	}
	
	//////////////////////////////////////////////////////////////////////////
	// The external interface methods for the GUI scripts.
	//
	
	/** Gets the identifier of this button. */
	public int GetIdentifier() {
		return m_identifier;
	}
	
	/** Gets the name of this button. */
	public string GetName() {
		return m_name;
	}
	
	/** Gets the position of this button (relative to the parent group). */
	public Vector2 GetPosition() {
		TPTabletUIConfig cfg = TPTabletUIManager.GetInstance().GetConfig();
		return m_position * cfg.GetScale();
	}
	
	/** Gets the size of this button. */
	public Vector2 GetSize() {
		TPTabletUIConfig cfg = TPTabletUIManager.GetInstance().GetConfig();
		return m_size * cfg.GetScale();
	}
	
	/** Gets the number of button data. */
	public int GetNumData() {
		return m_data.Count;
	}
	/** Sets the index of the currently selected button data. */
	public void SetDataSelection(int selection) {
		m_selection = selection;
	}
	/** Gets the index of the currently selected button data. */
	public int GetDataSelection() {
		return m_selection;
	}
	
	/** Sets the text string of the i-th button data. */
	public void SetDataTextString(int i, string textString) {
		Data data = m_data[i] as Data;
		data.SetTextString(textString);
	}
	/** Gets the text string of the i-th button data. */
	public string GetDataTextString(int i) {
		Data data = m_data[i] as Data;
		return data.GetTextString();
	}
	/** Gets the text color of the i-th button data. */
	public Color GetDataTextColor(int i) {
		Data data = m_data[i] as Data;
		return data.GetTextColor();
	}
	/** Gets the text size of the i-th button data. */
	public int GetDataTextSize(int i) {
		TPTabletUIConfig cfg = TPTabletUIManager.GetInstance().GetConfig();
		Data data = m_data[i] as Data;
		return (int)((float)data.GetTextSize() * cfg.GetScale());
	}
	/** Gets the text alignment of the i-th button data. */
	public TextAnchor GetDataTextAlignment(int i) {
		Data data = m_data[i] as Data;
		return data.GetTextAlignment();
	}
	/** Gets the normal image of the i-th button data. */
	public Texture2D GetDataNormalImage(int i) {
		Data data = m_data[i] as Data;
		return data.GetTextImage(0);
	}
	/** Gets the active image of the i-th button data. */
	public Texture2D GetDataActiveImage(int i) {
		Data data = m_data[i] as Data;
		return data.GetTextImage(1);
	}
	/** Gets the normal background color of the i-th button data. */
	public Color GetDataNormalBgColor(int i) {
		Data data = m_data[i] as Data;
		return data.GetBgColor(0);
	}
	/** Gets the active background color of the i-th button data. */
	public Color GetDataActiveBgColor(int i) {
		Data data = m_data[i] as Data;
		return data.GetBgColor(1);
	}
	/** Gets the event of the specified type, which is bound to the i-th button. */
	public TPTabletUIEvent GetDataEvent(int i, string type) {
		Data data = m_data[i] as Data;
		return data.GetEvent(type);
	}
	
	/** Gets the bounding rectangle of the button (absolute position and size). */
	public Rect GetRect() {
		TPTabletUIConfig cfg = TPTabletUIManager.GetInstance().GetConfig();
		float scale = cfg.GetScale();
		return new Rect(m_boundRect.x * scale, m_boundRect.y * scale,
			m_boundRect.width * scale, m_boundRect.height * scale);
	}
}

public class TPTabletUIGroup {
	private int	m_identifier = -1;
	private string	m_name;
	private Vector2	m_position = new Vector2(-1.0f, -1.0f);
	public enum Mode { All, Selection };
	private Mode	m_mode = Mode.All;
	private bool	m_enabled = true;
	private bool	m_orgEnabled = true;
	private int	m_selection = -1;
	private int	m_orgSelection = -1;
	private ArrayList	m_groups = new ArrayList();
	private ArrayList	m_buttons = new ArrayList();
	
	/** The absolute position and size (the bounding rectangle). */
	private Rect	m_boundRect = new Rect(0.0f, 0.0f, 0.0f, 0.0f);
	
	public TPTabletUIGroup(int identifier) {
		m_identifier = identifier;
	}
	
	public bool Parse(ConfigElement ce) {
		TPTabletUIManager mgr = TPTabletUIManager.GetInstance();
		TPTabletUIConfig cfg = mgr.GetConfig();
		
		m_name = ce.GetAttribute("name");
		
		string attrib = ce.GetAttribute("position");
		if(string.IsNullOrEmpty(attrib)){
			mgr.PutErrorMessage("Found a Group element ("
				+ GetName() + ") which doesn't have the position attribute.");
			return false;
		}else{
			string[] vec = attrib.Split(' ');
			if(vec.Length != 2){
				mgr.PutErrorMessage("Found an invalid position attribute (Group : "
					+ GetName() + ").");
				return false;
			}
			float.TryParse(vec[0], out m_position.x);
			float.TryParse(vec[1], out m_position.y);
		}
		
		attrib = ce.GetAttribute("mode");
		if(!string.IsNullOrEmpty(attrib)){
			if(attrib == "all") m_mode = Mode.All;
			else if(attrib == "selection") m_mode = Mode.Selection;
			else{
				mgr.PutErrorMessage("Found an invalid mode attribute (Group : "
					+ GetName() + ") : " + attrib);
				return false;
			}
		}
		
		int i;
		for(i=0; i<ce.GetNumChildren(); i++){
			ConfigElement child = ce.GetChild(i);
			string name = child.GetName();
			if(name == "Enable"){
				attrib = child.GetAttribute("value");
				if(!string.IsNullOrEmpty(attrib)){
					if(attrib == "true") m_enabled = true;
					else if(attrib == "false") m_enabled = false;
					else{
						mgr.PutErrorMessage("Found an invalid value attribute (Group/Enable : "
							+ GetName() + ") : " + attrib);
						return false;
					}
					m_orgEnabled = m_enabled;
				}
			}else if(name == "Selection"){
				attrib = child.GetAttribute("index");
				if(!string.IsNullOrEmpty(attrib)){
					int.TryParse(attrib, out m_selection);
					m_orgSelection = m_selection;
				}
			}else if(name == "Group"){
				TPTabletUIGroup grp = cfg.CreateGroup();
				if(grp.Parse(child))
					m_groups.Add(grp);
				else return false;
			}else if(name == "Button"){
				TPTabletUIButton btn = cfg.CreateButton();
				if(btn.Parse(child))
					m_buttons.Add(btn);
				else return false;
			}
		}
		
		return true;
	}
	
	public void CalculateRect(ref Stack positionStack, ref Vector2 maxCoords) {
		Vector2 p = (Vector2)positionStack.Peek() + m_position;
		positionStack.Push(p);
		int i;
		for(i=0; i<m_groups.Count; i++){
			TPTabletUIGroup grp = m_groups[i] as TPTabletUIGroup;
			grp.CalculateRect(ref positionStack, ref maxCoords);
		}
		positionStack.Pop();
		
		for(i=0; i<m_buttons.Count; i++){
			TPTabletUIButton btn = m_buttons[i] as TPTabletUIButton;
			btn.CalculateRect(p, ref maxCoords);
		}
		m_boundRect = Rect.MinMaxRect(p.x, p.y, maxCoords.x, maxCoords.y);
	}
	
	/** Resets all the "settable" attributes to their default values. */
	public void ResetAllAttributes() {
		SetEnable(m_orgEnabled);
		SetChildGroupSelection(m_orgSelection);
	}
	
	//////////////////////////////////////////////////////////////////////////
	// The external interface methods for the GUI scripts.
	//
	
	/** Gets the identifier of this group. */
	public int GetIdentifier() {
		return m_identifier;
	}
	
	/** Gets the name of this group. */
	public string GetName() {
		return m_name;
	}
	
	/** Gets the position of this group (relative to the parent group). */
	public Vector2 GetPosition() {
		TPTabletUIConfig cfg = TPTabletUIManager.GetInstance().GetConfig();
		return m_position * cfg.GetScale();
	}
	
	/** Gets the mode which describes how this group manages its child groups. */
	public Mode GetMode() {
		return m_mode;
	}
	
	/** Enables/disables (shows/hides) the group. */
	public void SetEnable(bool enabled) {
		m_enabled = enabled;
	}
	/** Checks if the group is enabled (shown). */
	public bool GetEnable() {
		return m_enabled;
	}
	
	/** Sets the index of the currently selected child group. */
	public void SetChildGroupSelection(int selection) {
		m_selection = selection;
	}
	/** Gets the index of the currently selected child group. */
	public int GetChildGroupSelection() {
		return m_selection;
	}
	
	/** Gets the number of child groups. */
	public int GetNumChildGroups() {
		return m_groups.Count;
	}
	/** Gets the i-th child group. */
	public TPTabletUIGroup GetChildGroup(int i) {
		return m_groups[i] as TPTabletUIGroup;
	}
	
	/** Gets the number of buttons which are owned by this group. */
	public int GetNumButtons() {
		return m_buttons.Count;
	}
	/** Gets the i-th button of those which are owned by this group. */
	public TPTabletUIButton GetButton(int i) {
		return m_buttons[i] as TPTabletUIButton;
	}
	
	/** Gets the bounding rectangle of the group (absolute position and size). */
	public Rect GetRect() {
		TPTabletUIConfig cfg = TPTabletUIManager.GetInstance().GetConfig();
		float scale = cfg.GetScale();
		return new Rect(m_boundRect.x * scale, m_boundRect.y * scale,
			m_boundRect.width * scale, m_boundRect.height * scale);
	}
}

public class TPTabletUIConfig {
	private ArrayList	m_networkEvents = new ArrayList();
	private ArrayList	m_rootGroups = new ArrayList();
	private ScreenOrientation	m_orientation = ScreenOrientation.LandscapeLeft;
	private float	m_scale = 1.0f;
	private Vector2	m_size = new Vector2(0.0f, 0.0f);
	/** The "flat" array of all the TPTabletUIGroup instances. */
	private ArrayList	m_allGroups = new ArrayList();
	/** The "flat" array of all the TPTabletUIButton instances. */
	private ArrayList	m_allButtons = new ArrayList();

	/** Updates the bounding rectangles of all the groups and buttons. */
	private void CalculateRect() {
		Stack positionStack = new Stack();
		positionStack.Push(new Vector2(0.0f, 0.0f));
		int i;
		for(i=0; i<m_rootGroups.Count; i++){
			TPTabletUIGroup grp = m_rootGroups[i] as TPTabletUIGroup;
			Vector2 maxCoords = new Vector2(0.0f, 0.0f);
			grp.CalculateRect(ref positionStack, ref maxCoords);
			if(maxCoords.x > m_size.x) m_size.x = maxCoords.x;
			if(maxCoords.y > m_size.y) m_size.y = maxCoords.y;
		}
	}
	
	public bool Parse(byte[] bytes) {
		TPTabletUIManager mgr = TPTabletUIManager.GetInstance();
		
		ConfigFile cf = new ConfigFile();
		if(!cf.Parse(bytes)) return false;
		ConfigElement root = cf.GetRoot();
		if(root.GetName() != "TabletInterface"){
			mgr.PutErrorMessage("The configuration file is invalid.");
			return false;
		}
		
		m_networkEvents = new ArrayList();
		m_rootGroups = new ArrayList();
		
		int i;
		for(i=0; i<root.GetNumChildren(); i++){
			ConfigElement ce = root.GetChild(i);
			string name = ce.GetName();
			if(name == "NetworkEvent"){
				TPTabletUIEvent networkEvent = new TPTabletUIEvent();
				if(networkEvent.Parse(ce))
					m_networkEvents.Add(networkEvent);
				else return false;
			}else if(name == "GUIDefinition"){
				ConfigElement guiDef = ce;
				string attrib = ce.GetAttribute("orientation");
				if(!string.IsNullOrEmpty(attrib)){
					if(attrib == "vertical") m_orientation = ScreenOrientation.Portrait;
					else if(attrib == "horizontal") m_orientation = ScreenOrientation.LandscapeLeft;
					else{
						mgr.PutErrorMessage("Found an invalid orientation attribute. (" + attrib + ")");
						return false;
					}
				}
				int j;
				for(j=0; j<guiDef.GetNumChildren(); j++){
					ce = guiDef.GetChild(j);
					if(ce.GetName() == "Group"){
						TPTabletUIGroup rootGroup = CreateGroup();
						if(rootGroup.Parse(ce))
							m_rootGroups.Add(rootGroup);
						else return false;
					}
				}
			}
		}
		
		CalculateRect();
		
		return true;
	}
	
	public TPTabletUIGroup CreateGroup() {
		int identifier = m_allGroups.Count;
		TPTabletUIGroup grp = new TPTabletUIGroup(identifier);
		m_allGroups.Add(grp);
		return grp;
	}
	
	public TPTabletUIButton CreateButton() {
		int identifier = m_allButtons.Count;
		TPTabletUIButton btn = new TPTabletUIButton(identifier);
		m_allButtons.Add(btn);
		return btn;
	}
	
	//////////////////////////////////////////////////////////////////////////
	// The external interface methods for the GUI scripts.
	//
	
	/** Finds the network event of the specified type. */
	public TPTabletUIEvent FindNetworkEvent(string type) {
		//TPTabletUIManager mgr = TPTabletUIManager.GetInstance();
		//mgr.PutErrorMessage("FindNetworkEvent(" + type + ")");
		int i;
		for(i=0; i<m_networkEvents.Count; i++){
			TPTabletUIEvent ev = m_networkEvents[i] as TPTabletUIEvent;
			if(ev.GetEventType() == type)
				return ev;
		}
		//mgr.PutErrorMessage("FindNetworkEvent(" + type + ") Failed to find event.");
		return null;
	}
	
	/** Finds the group of the specified name. */
	public TPTabletUIGroup FindGroupByName(string name) {
		int i;
		for(i=0; i<m_allGroups.Count; i++){
			TPTabletUIGroup grp = m_allGroups[i] as TPTabletUIGroup;
			if(grp.GetName() == name)
				return grp;
		}
		return null;
	}
	/** Finds the group of the specified identifier. */
	public TPTabletUIGroup FindGroupByIdentifier(int identifier) {
		return m_allGroups[identifier] as TPTabletUIGroup;
	}

	/** Finds the child group of the specified group and name. */
	public int FindChildGroupIdByName(TPTabletUIGroup grp, string name) {
		int numChildrGroups = grp.GetNumChildGroups();
		for(int i=0; i<numChildrGroups; i++){
			TPTabletUIGroup childGrp = grp.GetChildGroup(i);
			if(childGrp.GetName() == name)
				return i;
		}
		return -1;
	}

	/** Finds the button of the specified name. */
	public TPTabletUIButton FindButtonByName(string name) {
		int i;
		for(i=0; i<m_allButtons.Count; i++){
			TPTabletUIButton btn = m_allButtons[i] as TPTabletUIButton;
			if(btn.GetName() == name)
				return btn;
		}
		return null;
	}
	/** Finds the button of the specified identifier. */
	public TPTabletUIButton FindButtonByIdentifier(int identifier) {
		return m_allButtons[identifier] as TPTabletUIButton;
	}
	
	/** Gets the screen orientation. */
	public ScreenOrientation GetOrientation() {
		return m_orientation;
	}
	
	/** Sets the global scale which is applied to all the GUI elements. */
	public void SetScale(float scale) {
		m_scale = scale;
		CalculateRect();
	}
	/** Gets the global scale. */
	public float GetScale() {
		return m_scale;
	}
	
	/** Gets the bounding rectangle which contains all the root groups. */
	public Rect GetRect() {
		return new Rect(0.0f, 0.0f, m_size.x * m_scale, m_size.y * m_scale);
	}
	
	/** Gets the number of network events. */
	public int GetNumNetworkEvents() {
		return m_networkEvents.Count;
	}
	/** Gets the i-th network event. */
	public TPTabletUIEvent GetNetworkEvent(int i) {
		return m_networkEvents[i] as TPTabletUIEvent;
	}
	
	/** Gets the number of root groups. */
	public int GetNumRootGroups() {
		return m_rootGroups.Count;
	}
	/** Gets the i-th root group. */
	public TPTabletUIGroup GetRootGroup(int i) {
		return m_rootGroups[i] as TPTabletUIGroup;
	}
	
	/** Resets all the attributes of all the elements to their initial values. */
	public void ResetAllAttributes() {
		int i;
		for(i=0; i<m_allGroups.Count; i++)
			((TPTabletUIGroup)m_allGroups[i]).ResetAllAttributes();
		for(i=0; i<m_allButtons.Count; i++)
			((TPTabletUIButton)m_allButtons[i]).ResetAllAttributes();
	}
}
