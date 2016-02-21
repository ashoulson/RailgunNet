using UnityEngine;

public class UnityDebugListener : MonoBehaviour
{
  void Awake()
  {
    UnityDebugTraceListener.Awake();
  }
}

// singleton pattern
public class UnityDebugTraceListener : System.Diagnostics.TraceListener
{
  private static UnityDebugTraceListener instance;

  public static void Awake()
  {
    if (UnityDebugTraceListener.instance != null)
      return;
    UnityDebugTraceListener.instance = new UnityDebugTraceListener();

    System.Diagnostics.Debug.Listeners.Add(UnityDebugTraceListener.instance);
    System.Diagnostics.Debug.WriteLine("UnityDebugTraceListener Started");
  }

  public override void WriteLine(string message)
  {
    Debug.Log(message);
  }

  public override void Write(string message)
  {
    Debug.Log(message);
  }
}