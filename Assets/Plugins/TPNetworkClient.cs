using System;
using System.Text;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class TPNetworkClient {
	/** The IP address of the server. */
	private string	m_ipAddress;
	/** The port number for the session. */
	private int	m_port;
	/** The client-side interface of the TCP/IP connection. */
	private TcpClient	m_tcpClient;
	/** The data stream obtained from the TcpClient object. */
	private NetworkStream	m_networkStream;
	/** The data buffer for sending a message. */
	private byte[]	m_buffer;
	
	/** Initializes the instance. */
	public bool Initialize() {
		TPTabletUIManager mgr = TPTabletUIManager.GetInstance();
		
		// Set up the data buffer.
		m_buffer = new byte [256];
		int i;
		for(i=0; i<m_buffer.Length; i++)
			m_buffer[i] = 0;
		
		// Get the IP address and the port number from the Settings bundle.
		m_ipAddress = PlayerPrefs.GetString("ip_address");
		string portNumber = PlayerPrefs.GetString("port_number");
		if(m_ipAddress == "" || portNumber == ""){
			mgr.PutErrorMessage("Failed to get the IP address or the port number from Settings.bundle.");
			return false;
		}
		m_port = int.Parse(portNumber);
		
		// Try to connect to the server.
		try {
			m_tcpClient = new TcpClient(m_ipAddress, m_port);
			//m_tcpClient = new TcpClient("10.120.64.108", m_port);
			m_tcpClient.NoDelay = true;	// Disable buffering of the data.
			// Get the data stream.
			m_networkStream = m_tcpClient.GetStream();
		} catch(Exception e) {
			mgr.PutErrorMessage("Failed to connect to the server : " + e.Message);
			return false;
		}
	
		return true;
	}
	
	/** Terminates the instance. */
	public void Terminate() {
		// Let the server know the connection is going to be cut down soon.
		Send("#disconnect");
		if(m_networkStream != null){
			m_networkStream.Close();
			m_networkStream = null;
		}
		if(m_tcpClient != null){
			m_tcpClient.Close();
			m_tcpClient = null;
		}
		m_buffer = null;
	}
	
	/** Sends a string to the server. */
	public bool Send(string msg) {
		if(m_networkStream == null)
			return false;
		
		// Convert the given string to a byte array.
		byte[] bytes = Encoding.ASCII.GetBytes(msg);
		if(bytes.Length >= m_buffer.Length){
			Debug.LogError("The given message is too long to send. (" + msg + ")");
			return false;
		}
		int i;
		for(i=0; i<m_buffer.Length; i++){
			if(i < bytes.Length) m_buffer[i] = bytes[i];
			else m_buffer[i] = 0;
		}
		
		try {
			// Write the content of the buffer to the network data stream.
			m_networkStream.Write(m_buffer, 0, m_buffer.Length);
		} catch(Exception e) {
			Debug.LogError("Failed to send the given string : " + e.Message);
			return false;
		}
		
		return true;
	}
	
	/** Gets the IP address of the server. */
	public string GetIPAddress() {
		return m_ipAddress;
	}
	
	/** Gets the port number for the session. */
	public int GetPort() {
		return m_port;
	}
	
	/** Creates a zero-cleared buffer of the specified length. */
	private byte[] CreateBuffer(int length) {
		byte[] buffer = new byte[length];
		int i;
		for(i=0; i<buffer.Length; i++)
			buffer[i] = 0;
		return buffer;
	}
	
	/** Sends a request for the list of files which are used for setting up the tablet's GUI. */
	public string[] RequestGUIFileList() {
		TPTabletUIManager mgr = TPTabletUIManager.GetInstance();
		mgr.PutErrorMessage("TPNetworkClient::RequestGUIFileList");

		if(!Send("#requestguifilelist")) return null;

		// Receive the header part of the reply message.
		int bodyLength = 0, nFiles = 0;
		while(true){
			try {
				if(m_networkStream.DataAvailable){
					byte[] buffer = CreateBuffer(8);
					int index = 0;
					while(true){
						int nBytes = m_networkStream.Read(buffer, index, buffer.Length - index);
						if(nBytes == 0)
							// Although DataAvailable was true, we couldn't get anything from the stream.
							// Some error seems to have occurred.
							throw new Exception("Failed to receive the GUI file list (the header part).");
						else if((index + nBytes) == buffer.Length)
							break;	// Complete receiving a message.
						else index += nBytes;	// Need to receive the rest of the message.
					}
					// Decompose the received message into two 32-bit integers.
					nFiles = BitConverter.ToInt32(buffer, 0);
					bodyLength = BitConverter.ToInt32(buffer, 4);
					break;
				}else{
					// NOTE : We need to see if this actually works as we expected.
					Thread.Sleep(0);	// To avoid a busy loop.
				}
			}catch(Exception e){
				mgr.PutErrorMessage(e.Message);
				return null;
			}
		}
		
		if(nFiles == 0){
			mgr.PutErrorMessage("The GUI file list has no elements.");
			return null;
		}
		
		// Receive the body part of the reply message.
		string files;	// A string which contains comma-separated file names.
		while(true){
			try {
				if(m_networkStream.DataAvailable){
					byte[] buffer = CreateBuffer(bodyLength);
					int index = 0;
					while(true){
						int nBytes = m_networkStream.Read(buffer, index, buffer.Length - index);
						if(nBytes == 0)
							// Although DataAvailable was true, we couldn't get anything from the stream.
							// Some error seems to have occurred.
							throw new Exception("Failed to receive the GUI file list (the body part).");
						else if((index + nBytes) == buffer.Length)
							break;	// Complete receiving a message.
						else index += nBytes;	// Need to receive the rest of the message.
					}
					// Convert the received message into a string.
					StringBuilder strBldr = new StringBuilder();
					int i;
					for(i=0; i<buffer.Length; i++){
						if(buffer[i] == 0) break;
						strBldr.Append((char)buffer[i]);
					}
					files = strBldr.ToString();
					break;
				}else{
					// NOTE : We need to see if this actually works as we expected.
					Thread.Sleep(0);	// To avoid a busy loop.
				}
			}catch(Exception e){
				mgr.PutErrorMessage(e.Message);
				return null;
			}
		}
		
		string[] result = files.Split(',');
		if(result.Length != nFiles){
			mgr.PutErrorMessage("The number of files in the received GUI file list is invalid.");
			return null;
		}
		return result;
	}
	
	/** Sends a request for a GUI data file. */
	public byte[] RequestGUIFile(string file) {
		TPTabletUIManager mgr = TPTabletUIManager.GetInstance();
		mgr.PutErrorMessage("TPNetworkClient::RequestGUIFile");

		if(!Send("#requestguifile:" + file)) return null;

		// Receive the header part of the reply message.
		int dataLength = 0, bodyLength = 0;
		while(true){
			try {
				if(m_networkStream.DataAvailable){
					byte[] buffer = CreateBuffer(8);
					int index = 0;
					while(true){
						int nBytes = m_networkStream.Read(buffer, index, buffer.Length - index);
						if(nBytes == 0)
							// Although DataAvailable was true, we couldn't get anything from the stream.
							// Some error seems to have occurred.
							throw new Exception("Failed to receive a GUI file data (the header part).");
						else if((index + nBytes) == buffer.Length)
							break;	// Complete receiving a message.
						else index += nBytes;	// Need to receive the rest of the message.
					}
					// Decompose the received message into two 32-bit integers.
					dataLength = BitConverter.ToInt32(buffer, 0);
					bodyLength = BitConverter.ToInt32(buffer, 4);
					break;
				}else{
					// NOTE : We need to see if this actually works as we expected.
					Thread.Sleep(0);	// To avoid a busy loop.
				}
			}catch(Exception e){
				mgr.PutErrorMessage(e.Message);
				return null;
			}
		}
		
		while(true){
			try {
				if(m_networkStream.DataAvailable){
					byte[] buffer = CreateBuffer(bodyLength);
					int index = 0;
					while(true){
						int nBytes = m_networkStream.Read(buffer, index, buffer.Length - index);
						if(nBytes == 0)
							// Although DataAvailable was true, we couldn't get anything from the stream.
							// Some error seems to have occurred.
							throw new Exception("Failed to receive the GUI file list (the body part).");
						else if((index + nBytes) == buffer.Length)
							break;	// Complete receiving a message.
						else index += nBytes;	// Need to receive the rest of the message.
					}
					byte[] data = CreateBuffer(dataLength);
					Buffer.BlockCopy(buffer, 0, data, 0, dataLength);
					return data;
				}else{
					// NOTE : We need to see if this actually works as we expected.
					Thread.Sleep(0);	// To avoid a busy loop.
				}
			}catch(Exception e){
				mgr.PutErrorMessage(e.Message);
				break;
			}
		}
		
		return null;
	}
}
