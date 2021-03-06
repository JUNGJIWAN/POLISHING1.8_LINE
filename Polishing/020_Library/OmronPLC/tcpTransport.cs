﻿using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace mcOMRON
{
	/// <summary>
	/// 
	/// Version:	1.0
	/// Author:		Joan Magnet
	/// Date:		02/2015
	/// 
	/// transport layer for TCP communication
	/// 
	/// Do not requires to catch exceptions due this job is made by the IFinsCommand class.
	///
	/// This version uses Synchronous communication, you should use multitasking
	/// to avoid hanging the current thread. 
	/// </summary>
	public class tcpTransport : ITransport
	{
		#region **** properties

		/// <summary>
		/// returns the connection status
		/// </summary>
		public bool Connected
		{
			get { return (this._socket == null) ? false : this._socket.Connected; }
		}

		#endregion



		#region **** constructor

		/// <summary>
		/// constructor
		/// </summary>
		public tcpTransport()
		{
			// used to ping the PLC
			//
			this._ping = new Ping();

			// EndPoint parametres
			//
			this._endPoint = new IPEndPoint(0,0);
		}


		/// <summary>
		/// sets ip & port
		/// </summary>
		/// <param name="ip"></param>
		/// <param name="port"></param>
		public void SetTCPParams(IPAddress ip, int port)
		{
			this._endPoint.Address = ip;
			this._endPoint.Port = port;
		}

		#endregion



		#region **** connect /close

		/// <summary>
		/// open the communication with the plc
		/// </summary>
		/// <returns></returns>
		public bool Connect()
		{
			// build the socket object
			//
			this._socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			this._socket.SendTimeout    = this._timeout;
			this._socket.ReceiveTimeout = this._timeout;

			// try to connect
			//
			//this._socket.Connect(this._endPoint);
			//return this.Connected;

			//JUNG/20200324
			IAsyncResult result = this._socket.BeginConnect(this._endPoint, null, null);
			bool success = result.AsyncWaitHandle.WaitOne(5000, true);

			if(this._socket.Connected)
			{
				this._socket.EndConnect(result);
				return true; 
			}
			else
			{
				this._socket.Close();
				return false;
			}
		}


		/// <summary>
		/// close the socket
		/// </summary>
		/// <returns></returns>
		public void Close()
		{
			if (this._socket == null) return;
			if (this.Connected)
			{
				this._socket.Disconnect(false);
				this._socket.Close();
			}
			this._socket.Dispose();
			this._socket = null;
		}
		
		#endregion

	

		#region **** send & receive

		/// <summary>
		/// send a command through the transport layer
		/// </summary>
		/// <param name="command"></param>
		/// <param name="cmdLen"></param>
		public int Send(ref Byte[] command, int cmdLen)
		{
			if (!this.Connected)
			{
				return -1;
				//throw new Exception("Socket is not connected.");
			}
			
			// sends the command
			//
			int bytesSent = this._socket.Send(command, cmdLen, SocketFlags.None); 
			
			// it checks the number of bytes sent
			//
			if (bytesSent != cmdLen)
			{
				string msg = string.Format("Sending error. (Expected bytes: {0}  Sent: {1})"
											, cmdLen, bytesSent);
				throw new Exception(msg);
			}
			
			return bytesSent;
		}



		/// <summary>
		/// receives a response from the plc
		/// </summary>
		/// <param name="response"></param>
		/// <param name="respLen"></param>
		/// <returns></returns>
		public int Receive(ref Byte[] response, int respLen)
		{
			if (!this.Connected)
			{
				return -1;
				//throw new Exception("Socket is not connected.");
			}

			// receives the response, this is a synchronous method and can hang the process
			//
			int bytesRecv = this._socket.Receive(response, respLen, SocketFlags.None);

			// check the number of bytes received
			//
			if (bytesRecv != respLen)
			{
				string msg = string.Format("Receiving error. (Expected: {0}  Received: {1})", respLen, bytesRecv);
				throw new Exception(msg);
			}

			return bytesRecv;
		}
		
		#endregion


	
		#region **** ping

		/// <summary>
		/// send a ping to the plc
		/// </summary>
		/// <returns></returns>
		public bool Ping()
		{
			if (this._endPoint.Address == null) return false;
			
			this._pingReply = this._ping.Send(this._endPoint.Address, this._timeout);

			return (this._pingReply.Status == IPStatus.Success) ? true : false;
		}

		#endregion



		#region **** variables

		#region **** sockets

		// socket
		//
		private IPEndPoint _endPoint = null;
		private Socket     _socket   = null;

		// default timeout 2 seconds
		//
		private Int32 _timeout = 2000;

		#endregion



		#region **** ping objects

		// ping class 
		//
		private Ping _ping = null;
		private PingReply _pingReply = null;

		#endregion

		#endregion
	}
}
