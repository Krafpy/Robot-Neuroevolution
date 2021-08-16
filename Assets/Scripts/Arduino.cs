using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Ports;

public class Arduino : MonoBehaviour {

	SerialPort SearchArduino(){
		print("Searching for arduino...");
		string[] ports = SerialPort.GetPortNames();
		foreach(string port in ports){
			if(port != "COM1"){
				print("Arduino on " + port);
				return new SerialPort(port, 115200);
			}
		}
		print("Arduino not found");
		return null;
	}

	public void TransferData(float[] data){
		print("Start uploading");
		StartCoroutine(DataTransfer(data));
	}

	IEnumerator DataTransfer(float[] data){
		SerialPort port = SearchArduino();

		if(port != null){
			port.NewLine = "\n";

			print("Opening port");
			port.Open(); // On ouvre le port
			
			yield return new WaitUntil(() => (char)port.ReadChar() == (char)'Y');

			port.Write("S");

			yield return new WaitUntil(() => (char)port.ReadChar() == (char)'S');

			print("Writing data");
			foreach(float val in data){ // On envoi les valeurs une par une
				print(val);
				port.Write(val.ToString() + "f");
				yield return new WaitUntil(() => (char)port.ReadChar() == (char)'R');
			}

			port.Write("E");

			print("Close port");
			port.Close();
		}
	}
}