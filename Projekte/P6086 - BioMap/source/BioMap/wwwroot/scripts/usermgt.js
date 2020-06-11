
class UserMgt {
  // User management.
  constructor() {
    this.DivMain = null;
    this.UserId = "";
    this.UserFullName = "";
    this.TAN = "";
    this.PermTicket = "";
    this.Level = 0;
    this.MayUpload = false;
    this.MaySeeAoi = true;
    this.MayChangeCategory = false;
    this.CallBackClosed = null;
    this.readFromCookie();
  }

  static validateEMailAddr(sEMailAddr) {
    var re = /^(([^<>()[\]\\.,;:\s@\"]+(\.[^<>()[\]\\.,;:\s@\"]+)*)|(\".+\"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
    return re.test(sEMailAddr);
  }

  static setCookie(cname, cvalue, exdays=400) {
    var d = new Date();
    d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
    var expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
  }

  static getCookie(cname) {
    var name = cname + "=";
    var decodedCookie = decodeURIComponent(document.cookie);
    var ca = decodedCookie.split(';');
    for (var i = 0; i < ca.length; i++) {
      var c = ca[i];
      while (c.charAt(0) == ' ') {
        c = c.substring(1);
      }
      if (c.indexOf(name) == 0) {
        return c.substring(name.length, c.length);
      }
    }
    return "";
  }

  readFromCookie() {
    this.UserId = UserMgt.getCookie('UserId');
    this.UserFullName = UserMgt.getCookie('UserFullName');
    this.PermTicket = UserMgt.getCookie('UserPermTicket');
  }

  LogIn() {
    return new Promise((resolve, reject) => {
      try {
        CurrentUser.ApiCall('/api/usermgt.php',{
          method: 'POST',
          cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
          credentials: 'same-origin', // include, *same-origin, omit
        },[
          {name:'Cmd',value:'AuthUser'},
        ])
        .then((response) => {
          return response.json();
        })
        .then((json)=>{
          CurrentUser.Level = json.Level;
          let N100 = CurrentUser.Level / 100;
          let N10 = (CurrentUser.Level % 100) / 10;
          let N1 = CurrentUser.Level % 10;
          CurrentUser.MayUpload = (N100 >= 1);
          CurrentUser.MayChangeCategory = (N100 >= 5);
          let elUserLevel=document.getElementById('UserLevel');
          if (elUserLevel) {
            elUserLevel.value = CurrentUser.Level;
          }
          if (resolve) {
            resolve();
          }
        })
        .catch((error) => {
          if (reject) {
            reject(error);
          }
        })
        ;
      } catch (ex) {
        if (reject) {
          reject("" + ex);
        }
      }
    });
  }
  ApiCall(sPhpFile,fetchArgs,aArgs) {
    return new Promise((resolve, reject) => {
      try {
        var sUrlParams = window.location.search;
        aArgs.forEach((arg, index, array) => {
          sUrlParams += ((sUrlParams.length < 1) ? '?' : '&') + arg.name + "=" + arg.value;
        });
        // sUrlParams += ((sUrlParams.length < 1) ? '?' : '&') + "EMailAddr=" + CurrentUser.UserId;
        // sUrlParams += ((sUrlParams.length < 1) ? '?' : '&') + "PermTicket=" + CurrentUser.PermTicket;
        fetch(sPhpFile + sUrlParams, fetchArgs)
          .then((response) => {
            resolve(response);
          })
          .catch((error) => {
            if (reject) {
              reject(error);
            }
          })
          ;
      } catch (ex) {
        if (reject) {
          reject("" + ex);
        }
      }
    });
  }

  MaySeeCategory(cat) {
    if (this.Level>=cat) {
      return true;
    }
    return false;
  }

  static refreshAccessablility() {
    let sTAN = document.getElementById('UserMgtTAN').value.trim();
    let bUserAcknowledgeTerms = document.getElementById('UserAcknowledgeTerms').checked;
    document.getElementById('UserMgtRegister').disabled = !(
      sTAN.length >= 9
      &&
      bUserAcknowledgeTerms
    );
  }

  BeforeClose() {
    // Call callback.
    if (CurrentUser.CallBackClosed) {
      CurrentUser.CallBackClosed();
    }
  }

  BeforeOpen() {
    //CurrentUser.LogIn();
    document.getElementById('UserMgtRegister').disabled = true;
    document.getElementById('UserAcknowledgeTerms').onchange = (handlers,ev) => {
      UserMgt.refreshAccessablility();
    };
    document.getElementById('UserMgtTAN').onchange = (handlers,ev) => {
      UserMgt.refreshAccessablility();
    };
    // Access levels.
    {
      CurrentUser.ApiCall('/api/usermgt.php',{
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      },[
        {name:'Cmd',value:'GetAllUsers'},
      ])
        .then((response) => {
        return response.json();
      })
      .then((json)=>{
        let aUsers=json;
        let section=document.getElementById('UserMgt_AllUsersSection');
        section.style.visibility=(aUsers.length>=2)?'visible':'hidden';
        let table=document.getElementById('UserMgt_AllUsersTable');
        while (table.childElementCount>1) {
          table.childNodes[1].remove();
        }
        aUsers.forEach((user, index, array) => {
          let tr=document.createElement('tr');
          table.appendChild(tr);
          let tdEMailAddr=document.createElement('td');
          tdEMailAddr.innerHTML=user.EMailAddr;
          tr.appendChild(tdEMailAddr);
          let tdFullName=document.createElement('td');
          tdFullName.innerHTML=user.FullName;
          tr.appendChild(tdFullName);
          let tdLevel=document.createElement('td');
          tr.appendChild(tdLevel);
          let inputLevel=document.createElement('input');
          inputLevel.type='number';
          inputLevel.value=user.Level;
          tdLevel.appendChild(inputLevel);
          inputLevel.onchange = (handlers,ev) => {
            CurrentUser.ApiCall('/api/usermgt.php',{
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
            },[
              {name:'Cmd',value:'SetLevel'},
              {name:'EMailAddr',value:user.EMailAddr},
              {name:'Level',value:handlers.currentTarget.value},
            ])
              .then((response) => {
              return response.json();
            })
            .then((json)=>{
            })  
          };
        });
      });
    }
  }
  
  PrepareDisplay(divMain) {
    this.DivMain = divMain;
    this.DivMain.CodeBehind = this;
    fetch('usermgt.html')
      .then(data => data.text())
      .then(html => {
        this.DivMain.innerHTML = html;
        // Set values.
        document.getElementById('UserMgtEMailAddr').value = CurrentUser.UserId;
        document.getElementById('UserMgtFullName').value = CurrentUser.UserFullName;
        document.getElementById('UserLevel').value = CurrentUser.Level;
        document.getElementById('UserMgtTAN').value = "";
        // Process user inputs.
        document.getElementById('UserMgtEMailAddr').onchange = function() {
          CurrentUser.UserId=document.getElementById('UserMgtEMailAddr').value;
          UserMgt.setCookie('UserId', CurrentUser.UserId);
        }
        document.getElementById('UserMgtFullName').onchange = function() {
          CurrentUser.FullName=document.getElementById('UserMgtFullName').value;
          UserMgt.setCookie('UserMgtFullName', CurrentUser.FullName);
        }
        document.getElementById('UserMgtRequestTAN').onclick = function() {
          document.getElementById('UserMgtTAN').value = "";
          let sUrlParams = window.location.search;
          sUrlParams += ((sUrlParams.length < 1) ? '?' : '&') + "Cmd=" + "SendTAN";
          fetch('/api/usermgt.php' + sUrlParams, {
            method: 'POST',
          })
            .then((response) => {
              return response.json();
            })
            .then((json) => {
              let sMessage="Die TAN wurde in einer EMail an die Adresse " + json.EMailAddr + " gesendet. Prüfen Sie bitte Ihren Post-Eingang, geben Sie die TAN in das Eingabefeld ein und bestätigen Sie es.";
              if (json.ShellResult) {
                sMessage+="\n\n"+"Ergebnis: "+json.ShellResult;
              }
              alert(sMessage);
            });
        }
        document.getElementById('UserMgtRegister').onclick = function() {
          let sUrlParams = window.location.search;
          sUrlParams += ((sUrlParams.length < 1) ? '?' : '&') + "Cmd=" + "VerifyTAN";
          sUrlParams += ((sUrlParams.length < 1) ? '?' : '&') + "TAN=" + document.getElementById('UserMgtTAN').value.trim();
          sUrlParams += ((sUrlParams.length < 1) ? '?' : '&') + "UserAcknowledgeTerms=" + document.getElementById('UserAcknowledgeTerms').checked;
          document.getElementById('UserMgtTAN').value = "";
          UserMgt.refreshAccessablility();
          fetch('/api/usermgt.php' + sUrlParams, {
            method: 'POST',
          })
            .then((response) => {
              return response.json();
            })
            .then((json) => {
              if (json.FullName) {
                CurrentUser.FullName=json.FullName;
                document.getElementById('UserMgtFullName').value = CurrentUser.FullName;
                UserMgt.setCookie('UserMgtFullName', CurrentUser.FullName);
              }
              if (json.PermTicket) {
                CurrentUser.PermTicket=json.PermTicket;
                UserMgt.setCookie('UserPermTicket', CurrentUser.PermTicket);
              }
              {
                CurrentUser.Level=json.Level;
                document.getElementById('UserLevel').value = CurrentUser.Level;
              }
              CurrentUser.LogIn()
              .then(() => {
                if (json.PermTicket && json.Level>=100) {
                  alert("Sie haben jetzt die Zugangsstufe "+CurrentUser.Level);
                  selectMainTab('Home');
                } else {
                  alert("Die Anmeldung war nicht erfolgreich. Tragen Sie bitte die TAN ein, die Ihnen zuletzt per EMail zugesandt worden ist.");
                }
              })
              .catch((ex)=>{
                alert("Bei der Anmeldung ist ein Fehler aufgetreten: "+ex);
              });
            })
            ;
        }
      });
  }

}

var CurrentUser = new UserMgt();
