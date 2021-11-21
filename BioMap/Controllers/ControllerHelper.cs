using System;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BioMap
{
  public static class ControllerHelper
  {
    public static SessionData CreateSessionData(ControllerBase controllerBase, DataService ds) {
      var sd = new SessionData(ds);
      try {
        string sProject = "";
        if (controllerBase.Request.Query.ContainsKey("Project")) {
          sProject = controllerBase.Request.Query["Project"];
        }
        string sUser = "";
        if (controllerBase.Request.Query.ContainsKey("User")) {
          sUser = controllerBase.Request.Query["User"];
        }
        string sPermTicket = "";
        if (controllerBase.Request.Query.ContainsKey("PermTicket")) {
          sPermTicket = controllerBase.Request.Query["PermTicket"];
        }
        sd.CurrentUser.EMail = sUser;
        sd.CurrentUser.Project = sProject;
        ds.LoadUser(sd, sPermTicket, sd.CurrentUser);
      } catch {}
      return sd;
    }
  }
}
