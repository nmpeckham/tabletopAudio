#region --- License & Copyright Notice ---
/*
Copyright (c) 2005-2018 Jeevan James
All rights reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
#endregion

using Id3.Frames;
using System;
using System.Runtime.Serialization;

namespace Id3.Serialization.Surrogates
{
    internal sealed class UnknownFrameSurrogate : Id3FrameSurrogate<UnknownFrame>
    {
        protected override void GetFrameData(UnknownFrame frame, SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Id", frame.Id);
            info.AddValue("Data", Convert.ToBase64String(frame.Data));
        }

        protected override UnknownFrame SetObjectData(UnknownFrame frame, SerializationInfo info, StreamingContext context,
            ISurrogateSelector selector)
        {
            frame.Id = info.GetString("Id");
            frame.Data = Convert.FromBase64String(info.GetString("Data"));
            return frame;
        }
    }
}