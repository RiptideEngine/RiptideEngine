namespace RiptideEngine.Audio {
    public static unsafe class AudioListener {
        public static Vector3 Position {
            get {
                AudioEngine.AL!.GetListenerProperty(ListenerVector3.Position, out var pos);
                return pos;
            }
            set {
                AudioEngine.AL!.SetListenerProperty(ListenerVector3.Position, value);
            }
        }

        public static Vector3 Velocity {
            get {
                AudioEngine.AL!.GetListenerProperty(ListenerVector3.Velocity, out var vel);
                return vel;
            }
            set {
                AudioEngine.AL!.SetListenerProperty(ListenerVector3.Velocity, value);
            }
        }

        public static (Vector3 Look, Vector3 Up) Orientation {
            get {
                (Vector3, Vector3) output;
                AudioEngine.AL!.GetListenerProperty(ListenerFloatArray.Orientation, (float*)&output);
                return output;
            }
            set {
                AudioEngine.AL!.SetListenerProperty(ListenerFloatArray.Orientation, (float*)&value);
            }
        }
    }
}
