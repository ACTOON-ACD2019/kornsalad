﻿using System;
using System.Collections.Generic;
using System.Text;

namespace kornsalad
{
    /*
     * fields = [
            'effect',
            'cut',
            'image_properties',
            'project',
            'parameters',
            'created_at'
        ]

        {"effect": 1, "cut": 67, "image_properties": "{}", "project": 2, "parameters": "20,2,2", "created_at": "2019-12-10T01:37:06.017609Z"}
     */
    struct Task
    {
        public string effect_name { get; set; }
        public string parameters { get; set; }
        public string cut_file { get; set; }
        public string cut_pos_x { get; set; }
        public string cut_pos_y { get; set; }
        public string image_properties { get; set; } // canvas only
        public string project_resolution_width { get; set; }
        public string project_resolution_height { get; set; }
        public string created_at { get; set; }
    }

    struct Cut
    {
        public string file { get; set; }
        public string type { get; set; }
        public string sequence { get; set; }
        public string sub_sequence { get; set; }
        public string pos_x { get; set; }
        public string pos_y { get; set; }
    }

    struct RpcRequest
    {
        public Task[] tasks { get; set; }
        public Cut[] cuts { get; set; }
    }
}
