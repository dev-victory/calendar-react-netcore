import moment from "moment";

export function convertNotificationToDateTime (minutesToAdd, startDate) {
    var date = new Date(startDate);
    date.setMinutes((date.getMinutes()) - (parseInt(minutesToAdd)))
    return apiDateFormat(date.toString());
}

export function apiDateFormat (jsDate) {
    return moment.parseZone(jsDate).format("YYYY-MM-DDTHH:mm:ss");
}

export function calendarDateFormat() {
    return 'dd MMM yyyy hh:mm a';
}