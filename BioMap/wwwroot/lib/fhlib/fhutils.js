class fhutils {

  static ShowDenial(sDetails) {
    var sMessage = "Keine Berechtigung.";
    if (sDetails && sDetails.trim().length > 0) {
      sMessage += "\n\n" + sDetails;
    }
    sMessage += "\n\nWenden Sie sich an den Administrator der Webseite, um Ihre Zugangsberechtigung anzupassen.";
    alert(sMessage);
  }

  static GetDecimalFromDMS(aDMS, sRef) {
    let fLatOrLong=undefined;
    if (isNaN(aDMS[0])) {
      fLatOrLong = (aDMS[0].numerator / (aDMS[0].denominator)) + (aDMS[1].numerator / (60 * aDMS[1].denominator)) + (aDMS[2].numerator / (3600 * aDMS[2].denominator));
    } else {
      fLatOrLong = (aDMS[0]) + (aDMS[1] / 60) + (aDMS[2] / 3600);
    }
    if (sRef == "S" || sRef == "W") {
      fLatOrLong *= -1;
    }
    return fLatOrLong;
  }

  static parseExifDateTime(sExifDateTime) {
    var str = sExifDateTime.split(" ");
    //get date part and replace ':' with '-'
    var dateStr = str[0].replace(/:/g, "-");
    //concat the strings (date and time part)
    var properDateStr = dateStr + " " + str[1];
    //pass to Date
    return new Date(properDateStr);
  };

  static toModifiedISO(date,what) {
    if (date) {
      let mm = date.getMonth() + 1; // getMonth() is zero-based
      let dd = date.getDate();
      let HH = date.getHours();
      let MM = date.getMinutes();
      let SS = date.getSeconds();
      let d = [
        date.getFullYear(),
        (mm > 9 ? '' : '0') + mm,
        (dd > 9 ? '' : '0') + dd
      ].join('-');
      let t = [
        (HH > 9 ? '' : '0') + HH,
        (MM > 9 ? '' : '0') + MM,
        (SS > 9 ? '' : '0') + SS,
      ].join(':');
      if (what == 'd') {
        return d;
      } else if (what == 't') {
        return t;
      }
      return [d, t].join(' ');
    } else {
      return "";
    }
  };

  static dateToInput(date) {
    if (date) {
      return date.getFullYear() + '-' + ('0' + (date.getMonth() + 1)).substr(-2,2) + '-' + ('0' + date.getDate()).substr(-2,2);
    }
    return "";
  }
  static timeToInput(time) {
    if (time) {
      return  ('0' + (time.getHours())).substr(-2,2) + ':' + ('0' + time.getMinutes()).substr(-2,2);
    }
    return "";
  }

  /* For a given date, get the ISO week number
  *
  * Based on information at:
  *
  *    http://www.merlyn.demon.co.uk/weekcalc.htm#WNR
  *
  * Algorithm is to find nearest thursday, it's year
  * is the year of the week number. Then get weeks
  * between that date and the first day of that year.
  *
  * Note that dates in one year can be weeks of previous
  * or next year, overlap is up to 3 days.
  *
  * e.g. 2014/12/29 is Monday in week  1 of 2015
  *      2012/1/1   is Sunday in week 52 of 2011
  */
  static getWeekNumber(d) {
    // Copy date so don't modify original
    d = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
    // Set to nearest Thursday: current date + 4 - current day number
    // Make Sunday's day number 7
    d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay()||7));
    // Get first day of year
    var yearStart = new Date(Date.UTC(d.getUTCFullYear(),0,1));
    // Calculate full weeks to nearest Thursday
    var weekNo = Math.ceil(( ( (d - yearStart) / 86400000) + 1)/7);
    // Return array of year and week number
    return [d.getUTCFullYear(), weekNo];
  }

  static validateEMailAddr(sEMailAddr) {
    var re = /^(([^<>()[\]\\.,;:\s@\"]+(\.[^<>()[\]\\.,;:\s@\"]+)*)|(\".+\"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
    return re.test(sEMailAddr);
  }

  static isConnectionGood() {
    let goodConnection=false;
    let connection=navigator.connection;
    if (connection) {
      goodConnection=(connection.downlink>=1.0);
    }
    return goodConnection;
  }
  
  static setClassElement(domElement,sClassElement,bCondition) {
    if (bCondition && domElement.className.indexOf(sClassElement)==-1) {
      domElement.className+=" "+sClassElement;
    } else if (!bCondition && domElement.className.indexOf(sClassElement)>=0) {
      domElement.className=domElement.className.replace(" "+sClassElement,"");
    }
  }
  static toggleClassElement(domElement,sClassElement) {
    if (domElement.className.indexOf(sClassElement)==-1) {
      domElement.className+=" "+sClassElement;
    } else { 
      domElement.className=domElement.className.replace(" "+sClassElement,"");
    }
  }
}
