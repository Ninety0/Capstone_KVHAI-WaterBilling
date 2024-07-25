$(document).ready(function () {

    //event listener
    $(document).on('change', '#emp-search', function () {
        emppagination();
    })

    $(document).on('click', '.emppagination', function (event) {
        event.preventDefault();
        //const page = parseInt($(this).text(), 10);
        var emppage = parseInt($(this).data('emppagination'), 10);
        emppagination(emppage);
    });

    function emppagination(i = 1) {//Yung i is default pero pwedeng ibang letter ilagay dyan [i] lang nilalagay ko
        var _search = $('#emp-search').val();
        var _category = $('#category').val();
        var _isActive = $('#toggleSwitch').prop('checked');
        //var date = $('#selectedDateRange').val();
        //var start_date = moment(date.substring(0, 10));
        //var end_date = moment(date.substring(13, 23));

        var array = {
            search: _search.toString(),
            category: _category.toLowerCase(),
            page_index: i
            //DateStart: start_date.format('YYYY-MM-DD'),
            //DateEnd: end_date.format('YYYY-MM-DD')
        };

        //console.log(start_date.format('YYYY-MM-DD'));
        //console.log(end_date.format('YYYY-MM-DD'))
        //console.log(array);

        $.ajax({
            url: window.location.origin + '/adminaccount/EmployeePagination',
            type: "POST",
            data: array,
            success: function (response) {
                var result = $(response).find("#tableData").html();
                console.log(result);
                $('#tableData').html(result)
            },
            error: function (xhr, status, error_m) {
                alert(xhr.responseText);
            }
        });
    }
});