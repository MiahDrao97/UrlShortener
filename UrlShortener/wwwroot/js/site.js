// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function initFetchUrls() {
    console.log("Loading all urls...")
    $.ajax({
        method = "GET",
        url = "/urls/all?$top=20",
    })
}
