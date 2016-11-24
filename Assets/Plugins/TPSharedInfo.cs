using System;
using System.Text;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

public class TPRecvThread {
	/** Indicates if the thread is being stopped by the user. */
	private bool	m_stopped = false;
	/** The client-side interface of the TCP/IP connection. */
	private TcpClient	m_tcpClient;
	/** The data stream obtained from the TcpClient object. */
	private NetworkStream	m_networkStream;
	/** The length of the key part of a message. */
	const int	MSGKEYLENGTH = 64;
	/** The length of the value part of a message. */
	const int	MSGVALLENGTH = 448;
	/** The total length of a message. */
	const int	MSGLENGTH = MSGKEYLENGTH + MSGVALLENGTH;
	/** The data buffer for receiving a message. */
	private byte[]	m_buffer;
	/** The "lock" object used to synchronize the accesses to the member variables. */
	private readonly object	m_lock = new object();
	/** The hashtable for hosting the key-value pairs. */
	private Hashtable	m_keyValues;
	/** The control event queue. */
	private ArrayList	m_ctrlEvents;
	/** The "stopwatch" object which measures the elapsed time since we updated the hashtable. */
	private Stopwatch	m_stopwatch;
	/** The maxumum time interval (in milliseconds) between updates of the hashtable. */
	const long	MAXUPDINTERVAL = 3000;
	/** The last error message. */
	private string	m_lastErrMsg = "";

	private int m_isDebugMode;
	
	/** Initializes the member variables. */
	public bool Initialize(string ipAddress, int port) {
		// Try to connect to the server.
		try {
			m_tcpClient = new TcpClient(ipAddress, port);
			// Get the data stream for receiving data.
			m_networkStream = m_tcpClient.GetStream();
			m_networkStream.ReadTimeout = 10;	// timeout : 10 msec
		} catch(Exception e) {
			UnityEngine.Debug.LogError("Failed to connect to the server : " + e.Message);
			return false;
		}
		
		// Set up the data buffer.
		ClearBuffer();
		
		m_keyValues = new Hashtable();
		m_ctrlEvents = new ArrayList();

		m_stopwatch = new Stopwatch();
		m_stopwatch.Start();
		
		return true;
	}
	
	/** Cleans up the member variables. */
	public void Terminate() {
		m_stopwatch = null;
		m_keyValues = null;
		m_ctrlEvents = null;
		m_buffer = null;
		if(m_networkStream != null){
			m_networkStream.Close();
			m_networkStream = null;
		}
		if(m_tcpClient != null){
			m_tcpClient.Close();
			m_tcpClient = null;
		}
		m_stopped = false;
		m_lastErrMsg = "";
	}
	
	/** Checks if the user is trying to stop the thread. */
	private bool IsStopped() {
		bool stopped = false;
		Monitor.Enter(m_lock);
		try {
			stopped = m_stopped;
		} finally {
			Monitor.Exit(m_lock);
		}
		return stopped;
	}
	
	/** Fills the data buffer with zeros. */
	private void ClearBuffer() {
		if(m_buffer == null)
			m_buffer = new byte[MSGLENGTH];
		int i;
		for(i=0; i<m_buffer.Length; i++)
			m_buffer[i] = 0;
	}
	
	/** The thread function. */
	public void Run() {
		//TPTabletUIManager mgr = TPTabletUIManager.GetInstance();
		//m_isDebugMode = PlayerPrefs.GetInt("debug_mode");
		// Used for decomposition of the received messages.
		StringBuilder keyStrBldr = new StringBuilder();
		StringBuilder valStrBldr = new StringBuilder();
		
		// The main loop of the thread.
		while(!IsStopped() /* See if the thread is being stopped by the user. */ ){
			try {
				if(m_networkStream.DataAvailable){
					ClearBuffer();	// Fill the buffer with zeros.
					int index = 0;
					while(true){
						int nBytes = m_networkStream.Read(m_buffer, index, m_buffer.Length - index);
						if(nBytes == 0)
							// Although DataAvailable was true, we couldn't get anything from the stream.
							// Some error seems to have occurred.
							throw new Exception("Failed to receive a message.");
						else if((index + nBytes) == m_buffer.Length)
							break;	// Complete receiving a message.
						else index += nBytes;	// Need to receive the rest of the message.
					}
					// Decompose the received message into the key and value strings.
					keyStrBldr.Length = 0;	// Clear the old content.
					int i;
					for(i=0; i<MSGKEYLENGTH; i++){
						if(m_buffer[i] == 0) break;
						keyStrBldr.Append((char)m_buffer[i]);
					}
					valStrBldr.Length = 0;	// Clear the old content.
					byte[] valBuffer = new byte[MSGLENGTH-MSGKEYLENGTH];		
					Array.Copy (m_buffer, MSGKEYLENGTH, valBuffer, 0, MSGLENGTH-MSGKEYLENGTH);
					string str = Encoding.Unicode.GetString(valBuffer);
					valStrBldr.Append(str);
					
					string key = keyStrBldr.ToString();
					string val = valStrBldr.ToString();
					if(key == "tablet_control") {
						AddControlEvent(val);	// Received a control event. Add it to the queue.
						//if( m_isDebugMode == 1 )
						//	mgr.PutErrorMessage("tablet_control : " + val);
					} else SetKeyValue(key, val);	// Update the hash table.
				}else{
					// NOTE : We need to see if this actually works as we expected.
					Thread.Sleep(0);	// To avoid a busy loop.
				}
			}catch(Exception e){
				SetLastError(e.Message);
				break;	// Exit the thread.
			}
		}
	}
	
	/** Lets the thread know that its owner is trying to stop it. */
	public void Stop() {
		Monitor.Enter(m_lock);
		try {
			m_stopped = true;
		} finally {
			Monitor.Exit(m_lock);
		}
	}
	
	/** Inserts an element to the hashtable, or updates an element in the hashtable. */
	private void SetKeyValue(string keyStr, string valStr) {
		Monitor.Enter(m_lock);
		try {
			m_keyValues[keyStr] = valStr;
			// Now we've updated the hashtable. Let us reset the stopwatch.
			m_stopwatch.Reset();
			m_stopwatch.Start();
		} finally {
			Monitor.Exit(m_lock);
		}
	}
	
	/** Gets the value which corresponds to the given keyword. */
	public string GetKeyValue(string keyStr) {
		string valStr = null;
		Monitor.Enter(m_lock);
		try {
			if(m_keyValues.ContainsKey(keyStr))
				valStr = m_keyValues[keyStr] as string;
		} finally {
			Monitor.Exit(m_lock);
		}
		return valStr;
	}
	
	/** Appends a cotrol event to the queue. */
	private void AddControlEvent(string ctrlEvent) {
		Monitor.Enter(m_lock);
		try {
			m_ctrlEvents.Add(ctrlEvent);
			
		} finally {
			Monitor.Exit(m_lock);
		}
	}
	
	/** Gets and removes all the control events from the queue. */
	public ArrayList GetControlEvents() {
		ArrayList ctrlEvents = new ArrayList();
		Monitor.Enter(m_lock);
		try {
			ctrlEvents = (ArrayList)m_ctrlEvents.Clone();
			m_ctrlEvents.Clear();
		} finally {
			Monitor.Exit(m_lock);
		}
		return ctrlEvents;
	}
	
	/** Sets the given string to the last error message. */
	private void SetLastError(string str) {
		Monitor.Enter(m_lock);
		try {
			m_lastErrMsg = str;
		} finally {
			Monitor.Exit(m_lock);
		}
	}
	
	/** Gets the last error message. */
	public string GetLastError() {
		string errMsg = null;
		Monitor.Enter(m_lock);
		try {
			errMsg = m_lastErrMsg;
		} finally {
			Monitor.Exit(m_lock);
		}
		return errMsg;
	}
	
	/** Checks if the connection to the server is still alive. */
	public bool IsConnected() {
		bool connected = false;
		Monitor.Enter(m_lock);
		try {
			long t = m_stopwatch.ElapsedMilliseconds;
			connected = (t <= MAXUPDINTERVAL) ? true : false;
		} finally {
			Monitor.Exit(m_lock);
		}
		return connected;
	}
}

public class TPSharedInfo {
	/** The TPRecvThread class instance. */
	private TPRecvThread	m_recvThread;
	/** The thread controller. */
	private Thread	m_threadHandler;
	/** The array of control events. */
	private ArrayList	m_ctrlEvents = null;
	
	/** Initializes the member variables, and starts the thread. */
	public bool Initialize(string ipAddress, int port) {
		if(m_threadHandler != null){
			UnityEngine.Debug.LogError("The TPSharedInfo instance has already been initialized.");
			return false;
		}
		// Set up and start the message receiving thread.
		m_recvThread = new TPRecvThread();
		if(!m_recvThread.Initialize(ipAddress, port)){
			m_recvThread.Terminate();
			m_recvThread = null;
			return false;
		}
		try {
			m_threadHandler = new Thread(new ThreadStart(m_recvThread.Run));
			m_threadHandler.Start();
		} catch(Exception e) {
			UnityEngine.Debug.LogError("Failed to start the message receiving thread : " + e.Message);
			m_threadHandler = null;
			return false;
		}
			
		return true;
	}
	
	/** Stops the thread, and cleans up the member variables. */
	public void Terminate() {
		if(m_threadHandler == null) return;
		
		// Let the message receiving thread know that the user is trying to stop it.
		m_recvThread.Stop();
		// Wait the end of the thread.
		m_threadHandler.Join();
		
		m_recvThread.Terminate();
		m_recvThread = null;
		m_threadHandler = null;
		m_ctrlEvents = null;
	}
	
	/** Get the value string which corresponds to the given key string. */
	public string Get(string keyStr) {
		return (m_recvThread == null) ? null : m_recvThread.GetKeyValue(keyStr);
	}
	
	/** Checks if the connection to the server is still alive. */
	public bool IsConnected() {
		if(m_recvThread == null || m_threadHandler == null) return false;
		
		// Check if the message receiving thread is still running.

		if( m_threadHandler.ThreadState != System.Threading.ThreadState.Running ) {
			if(m_threadHandler.Join(0)){
				// The thread ended unexpectedly for some reason.
				string errMsg = m_recvThread.GetLastError();
				UnityEngine.Debug.LogError("The message receiving thread ended unexpectedly : " + errMsg);
				return false;
			}
		}
		
		// Let the message receiving thread check if the connection is still alive.
		return m_recvThread.IsConnected();
	}
	
	/**
	 * Sees if there are any control events in the queue, and gets the number of them.
	 * @return Zero means no events.
	 */
	public int CheckControlEvents() {
		if(m_recvThread == null) return 0;
		m_ctrlEvents = m_recvThread.GetControlEvents();
		return (m_ctrlEvents == null) ? 0 : m_ctrlEvents.Count;
	}
	/** Gets the i-th control event in the queue, without removing it. */
	public string GetControlEvent(int i) {
		//TPTabletUIManager mgr = TPTabletUIManager.GetInstance();
		//int isDebugMode = PlayerPrefs.GetInt("debug_mode");

		if(m_ctrlEvents == null) {
//			if( isDebugMode == 1 )
//				mgr.PutErrorMessage("m_ctrlEvents == null");
			return null;
		}
		if(m_ctrlEvents.Count <= i) {
//			if( isDebugMode == 1 )
//				mgr.PutErrorMessage("m_ctrlEvents.Count <= i");
			return null;
		}
//		if( isDebugMode == 1 )
//			mgr.PutErrorMessage("GetControlEvent(" + i + ") -> " + m_ctrlEvents[i] as string );
		return m_ctrlEvents[i] as string;
	}
	/** Removes all the control events in the queue. */
	public void ResetControlEvent()	{
		m_ctrlEvents.Clear();
		m_ctrlEvents = null;
	}
}
