﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class Line
{
    public Vector3 start;
    public Vector3 end;
}

[ExecuteInEditMode]
public class CNCSculpt : MonoBehaviour {

    public Bounds area;
    public float verticalStep=0.1f;
    public float xzStep=0.01f;
    public float bitRadius = 0.005f;
    public bool secondPass = false;
    
    float[] depthmap = null;
    List<Line> lines;

    public bool go = false;

    public bool save = false;
    public string filename = "test.nc";

	string stringFloat(float f){
		return f.ToString ("###0.#");
	}
    
	void Update () {

        if (save)
        {
            save = false;
            StreamWriter file = new StreamWriter(Application.dataPath + "/" + filename);
            //file.WriteLine("(Generated by LUnityCNC v0.1)");
            //file.WriteLine("G21 G90 G40");
            //file.WriteLine("(lined part)");
            //file.WriteLine("G0 Z150");
            //file.WriteLine("T1 M6");
            //file.WriteLine("G17");
            //file.WriteLine("M3");
            file.WriteLine("G1 Z10");
            file.WriteLine("G1 X"+stringFloat(lines[0].start.x*10f)+" Y"+stringFloat(lines[0].start.y*10f));
			file.WriteLine("G1 Z"+stringFloat(lines[0].end.y*10f));
            foreach(Line L in lines){
                //file.WriteLine("G1 X" + L.start.x * 10f + " Y" + L.start.z * 10f + " Z" + L.start.y * 10f);
				file.WriteLine("G1 X" + stringFloat(L.end.x * 10f) + " Y" + stringFloat(L.end.z * 10f) + " Z" + stringFloat(L.end.y * 10f));

            }
            file.WriteLine("G1 Z10");
            //file.WriteLine("G0 Z150");
            //file.WriteLine("M5");
            //file.WriteLine("M30");

            file.Close();
        }


        if (go)
        {
            go = false;
            int x_steps = Mathf.CeilToInt(area.extents.x * 2f / xzStep);
            int z_steps = Mathf.CeilToInt(area.extents.x * 2f / xzStep);
            depthmap = new float[(x_steps+1)*(z_steps+1)];
            Vector3 origin = Vector3.zero;
            float distance = area.extents.y*2f;
            lines = new List<Line>();
            
            for (int x = 0; x <= x_steps; x++)
            {
                Line currentLine= null;

                int lineLength = 1;

                for (int z = 0; z <= z_steps; z++)
                {
                    int sz = z;
                    if(x%2==1) sz = z_steps-z;

                    Vector3 lastOrigin = origin;
                    origin = area.center + new Vector3(area.extents.x * ((float)x / (float)x_steps - 0.5f), area.extents.y, area.extents.z * ((float)sz / (float)z_steps - 0.5f));

                    depthmap[x + z * x_steps] = cutDepth(origin, area.extents.y*2f);

                    if (z < 1)
                    {
                        currentLine = new Line();
                        currentLine.start = origin + Vector3.up * depthmap[x + z * x_steps];
                        lines.Add(currentLine);
                    }else{
                        
                        currentLine.end = lastOrigin + Vector3.up * depthmap[x + (z-1) * x_steps];
                    }

                    if (z > 2)
                    {
                        float newslope = (depthmap[x + z * x_steps] - depthmap[x + (z - 1) * x_steps]);
                        float lastslope = (depthmap[x + (z - 1) * x_steps] - depthmap[x + (z - 2) * x_steps]);

                        if (Mathf.Abs(newslope - lastslope) < 0.001f)
                        {
                            lineLength++;
                        }
                        else
                        {
                            currentLine = new Line();
                            lines.Add(currentLine);
                            currentLine.start = lastOrigin + Vector3.up * depthmap[x + (z-1) * x_steps];
                            lineLength = 1;
                        }

                    }
                }
            }


            if (secondPass)
            {

                for (int z = 0; z <= z_steps; z++)
                {
                    Line currentLine = null;

                    int lineLength = 1;

                    for (int x = 0; x <= x_steps; x++)
                    {
                        int sx = x;
                        if (z % 2 == 0) sx = x_steps - x;

                        Vector3 lastOrigin = origin;
                        origin = area.center + new Vector3(area.extents.x * ((float)(sx) / (float)x_steps - 0.5f), area.extents.y, area.extents.z * ((float)z / (float)z_steps - 0.5f));

                        depthmap[x + z * x_steps] = cutDepth(origin, area.extents.y);

                        if (x < 1)
                        {
                            currentLine = new Line();
                            currentLine.start = origin + Vector3.up * depthmap[x + z * x_steps];
                            lines.Add(currentLine);
                        }
                        else
                        {

                            currentLine.end = lastOrigin + Vector3.up * depthmap[x - 1 + z * x_steps];
                        }

                        if (x > 2)
                        {
                            float newslope = (depthmap[x + z * x_steps] - depthmap[x - 1 + z * x_steps]);
                            float lastslope = (depthmap[x - 1 + z * x_steps] - depthmap[x - 2 + z * x_steps]);

                            if (Mathf.Abs(newslope - lastslope) < 0.001f)
                            {
                                lineLength++;
                            }
                            else
                            {
                                currentLine = new Line();
                                lines.Add(currentLine);
                                currentLine.start = lastOrigin + Vector3.up * depthmap[x - 1 + z * x_steps];
                                lineLength = 1;
                            }

                        }
                    }
                }
            }
        }

	}

    float downPoint(Vector3 origin, float maxlength)
    {
        RaycastHit hit;

        if (Physics.Raycast(origin, Vector3.down, out hit, maxlength, 0xffff))
        {
            return hit.point.y - origin.y;
        }
        else
        {
            return -maxlength;
        }
    }

    float cutDepth(Vector3 origin, float maxlength)
    {
        float highest = downPoint(origin, maxlength);

        for (float angle = 0; angle < Mathf.PI * 4f; angle += Mathf.PI / 16)
        {
            float border = 0.07f + downPoint(origin + (Vector3.left * Mathf.Sin(angle) + Vector3.forward * Mathf.Cos(angle)) * bitRadius, maxlength);
            if (border > highest) highest = border;
        }

        return highest;
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(area.center, area.extents);
        
        int x_steps = Mathf.CeilToInt(area.extents.x * 2f / xzStep);
        int z_steps = Mathf.CeilToInt(area.extents.x * 2f / xzStep);
        Vector3 origin;
        Vector3 lastOrigin = Vector3.zero;

        if (lines != null)
        {
            
            foreach (Line L in lines)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(L.start, L.end);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(L.start, L.start + Vector3.up * 0.01f);
                Gizmos.DrawLine(L.end, L.end + Vector3.up * 0.01f);
            }
        }
    }
}
