// This file was generated by a tool; you should avoid making direct changes.
// Consider using 'partial classes' to extend these types
// Input: prediction_output_evaluation.proto

#pragma warning disable 0612, 1591, 3021
namespace apollo.prediction
{

    [global::ProtoBuf.ProtoContract()]
    public partial class TrajectoryEvaluationMetrics : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);
        }
        public TrajectoryEvaluationMetrics()
        {
            OnConstructor();
        }

        partial void OnConstructor();

        [global::ProtoBuf.ProtoMember(1, IsRequired = true)]
        public double recall { get; set; }

        [global::ProtoBuf.ProtoMember(2, IsRequired = true)]
        public double precision { get; set; }

        [global::ProtoBuf.ProtoMember(3, IsRequired = true)]
        public double sum_squared_error { get; set; }

        [global::ProtoBuf.ProtoMember(4, IsRequired = true)]
        public int num_frame_obstacle { get; set; }

        [global::ProtoBuf.ProtoMember(5, IsRequired = true)]
        public int num_predicted_trajectory { get; set; }

        [global::ProtoBuf.ProtoMember(6, IsRequired = true)]
        public int num_future_point { get; set; }

        [global::ProtoBuf.ProtoMember(7)]
        [global::System.ComponentModel.DefaultValue(0)]
        public double num_correctly_predicted_frame_obstacle
        {
            get { return __pbn__num_correctly_predicted_frame_obstacle ?? 0; }
            set { __pbn__num_correctly_predicted_frame_obstacle = value; }
        }
        public bool ShouldSerializenum_correctly_predicted_frame_obstacle()
        {
            return __pbn__num_correctly_predicted_frame_obstacle != null;
        }
        public void Resetnum_correctly_predicted_frame_obstacle()
        {
            __pbn__num_correctly_predicted_frame_obstacle = null;
        }
        private double? __pbn__num_correctly_predicted_frame_obstacle;

        [global::ProtoBuf.ProtoMember(8)]
        [global::System.ComponentModel.DefaultValue("")]
        public string situation
        {
            get { return __pbn__situation ?? ""; }
            set { __pbn__situation = value; }
        }
        public bool ShouldSerializesituation()
        {
            return __pbn__situation != null;
        }
        public void Resetsituation()
        {
            __pbn__situation = null;
        }
        private string __pbn__situation;

        [global::ProtoBuf.ProtoMember(9, IsRequired = true)]
        public double time_range { get; set; }

        [global::ProtoBuf.ProtoMember(10, IsRequired = true)]
        public double min_time_range { get; set; }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class TrajectoryEvaluationMetricsGroup : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);
        }
        public TrajectoryEvaluationMetricsGroup()
        {
            OnConstructor();
        }

        partial void OnConstructor();

        [global::ProtoBuf.ProtoMember(1)]
        public TrajectoryEvaluationMetrics junction_metrics { get; set; }

        [global::ProtoBuf.ProtoMember(2)]
        public TrajectoryEvaluationMetrics on_lane_metrics { get; set; }

    }

}

#pragma warning restore 0612, 1591, 3021