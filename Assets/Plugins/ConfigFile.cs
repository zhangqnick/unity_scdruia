using System;
using System.IO;
using System.Xml;
using System.Collections;
using UnityEngine;

/** The class of an element of the XML configuration file. */
public class ConfigElement {
	/** The name of the element. */
	private	string	m_name = null;
	/** The element's "depth" in the hierarchy. */
	private int	m_depth = -1;
	/** The hashtable which contains pairs of attribute name and value. */
	private Hashtable	m_attributes = null;
	/** The array of child elements. */
	private ArrayList	m_children = null;
	/** The parent element. */
	private ConfigElement	m_parent = null;
	
	/** Parses the given XML element, and extracts all the attributes. */
	public void Parse(XmlReader reader, ConfigElement parent) {
		m_name = reader.LocalName;
		m_depth = reader.Depth;
		m_attributes = new Hashtable();
		m_children = new ArrayList();
		m_parent = parent;
		int i;
		for(i=0; i<reader.AttributeCount; i++){
			reader.MoveToAttribute(i);
			m_attributes[reader.Name] = reader.Value;
		}
		reader.MoveToElement();
		if(reader.Read()){
			while(true){
				if(reader.NodeType != XmlNodeType.Element){
					if(!reader.Read()) break;
					continue;
				}
				if(reader.Depth > m_depth){
					// Detected a child element.
					ConfigElement child = new ConfigElement();
					child.Parse(reader, this);
					m_children.Add(child);
				}else break;
			}
		}
	}
	/** Gets the name of the element. */
	public string GetName() { return m_name; }
	/** Gets the attribute value of the specified attribute name. */
	public string GetAttribute(string name) {
		return ((m_attributes != null && m_attributes.ContainsKey(name)) ? m_attributes[name] : null) as string;
	}
	/** Gets the number of child elements. */
	public int GetNumChildren() {
		return (m_children == null) ? 0 : m_children.Count;
	}
	/** Gets the i-th child element. */
	public ConfigElement GetChild(int i) {
		return ((m_children == null || i < 0 || i >= m_children.Count) ? null : m_children[i]) as ConfigElement;
	}
	/** Finds an element of the specified name from the hierarchy under this element. */
	public ConfigElement FindElement(string name) {
		int i;
		for(i=0; i<GetNumChildren(); i++){
			ConfigElement child = GetChild(i);
			if(child.GetName() == name) return child;
			ConfigElement descendant = child.FindElement(name);
			if(descendant != null)
				return descendant;
		}
		return null;
	}
	/** Gets the parent element.  */
	public ConfigElement GetParent() { return m_parent; }
}

/** The class of an XML configuration file. */
public class ConfigFile {
	/** The root XML element. */
	private ConfigElement	m_root = null;
	
	/** Parses the specified XML configuration file, and extracts all the elements. */
	public bool Parse(byte[] bytes) {
		TPTabletUIManager mgr = TPTabletUIManager.GetInstance();
		mgr.PutErrorMessage("ConfigFile::Parse");

		// Create the XmlReader instance for the specified file.
		XmlReaderSettings settings = new XmlReaderSettings();
		settings.ConformanceLevel = ConformanceLevel.Document;
		XmlReader reader = null;
		try {
			reader = XmlReader.Create(new MemoryStream(bytes), settings);
			while(reader.Read()){
				if(reader.NodeType != XmlNodeType.Element) continue;
				if(reader.Depth == 0){
					// Detected the root element.
					m_root = new ConfigElement();
					m_root.Parse(reader, null);
				}
			}
		}catch(Exception e){
			mgr.PutErrorMessage(e.Message);
			return false;
		}
		return true;
	}
	/** Gets the ConfigElement instance of the root XML element. */
	public ConfigElement GetRoot() { return m_root; }
}
