using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using UnityEngine.UI;

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Text;

public class UnityConnect : MonoBehaviour
{
    private WebCamTexture webcamTexture;
    private Texture2D texture;
    private Socket clientSocket;
    private const string serverIp = "127.0.0.1"; // Python server address
    private const int serverPort = 65432; // Server port

    public RawImage rawImage; // Drag the RawImage UI element in the Inspector

    void Start()
    {
        // Start webcam
        webcamTexture = new WebCamTexture();
        webcamTexture.Play();

        StartCoroutine(InitializeTexture());

        // Connect to the Python server
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            clientSocket.Connect(serverIp, serverPort);
            Debug.Log("Connected to Python server.");
        }
        catch (SocketException ex)
        {
            Debug.LogError($"Connection failed: {ex.Message}");
        }
    }

    IEnumerator InitializeTexture()
    {
        // Wait until webcam is initialized with valid width & height
        while (webcamTexture.width < 100)
            yield return null;

        texture = new Texture2D(webcamTexture.width, webcamTexture.height);

        // Display webcam feed on RawImage
        rawImage.texture = webcamTexture;
        rawImage.material.mainTexture = webcamTexture;

        Debug.Log($"Webcam started: {webcamTexture.width}x{webcamTexture.height}");
    }

    void Update()
    {
        if (webcamTexture != null && webcamTexture.isPlaying && clientSocket.Connected)
        {
            // Capture current frame
            texture.SetPixels(webcamTexture.GetPixels());
            texture.Apply();

            // Convert frame to byte array
            byte[] frameBytes = texture.EncodeToJPG();

            try
            {
                // Send frame size first
                byte[] sizeBytes = BitConverter.GetBytes(frameBytes.Length);
                clientSocket.Send(sizeBytes);

                // Send the actual frame
                clientSocket.Send(frameBytes);

                // Receive gesture name from server
                byte[] buffer = new byte[1024];
                int bytesRead = clientSocket.Receive(buffer);
                string gestureName = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log($"Recognized Gesture: {gestureName}");
            }
            catch (SocketException ex)
            {
                Debug.LogError($"Socket error: {ex.Message}");
            }
        }
    }

    void OnApplicationQuit()
    {
        if (clientSocket != null && clientSocket.Connected)
        {
            clientSocket.Close();
            Debug.Log("Connection closed.");
        }

        if (webcamTexture != null && webcamTexture.isPlaying)
            webcamTexture.Stop();
    }
}
