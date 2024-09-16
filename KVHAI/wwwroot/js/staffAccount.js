$(document).ready(function () {

    // Call GetCurrentTab on page load
    $(document).ready(function () {
        GetCurrentTab();
    });

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

    /*   NAVBAR    */
    // Hide all sections initially
    $('section').hide();

    // Handle click events on header links
    $('.link-header').click(function (e) {
        e.preventDefault();
        $('.link-header').removeClass('active'); // Remove 'active' class from all links
        $(this).addClass('active'); // Add 'active' class to clicked link
        var targetSection = $(this).attr('href'); // Get the target section from the href attribute
        localStorage.setItem("tab", targetSection);
        $('section').hide(); // Hide all sections
        $(targetSection).show(); // Show the target section
    });

    // Function to get and show the current tab
    function GetCurrentTab() {
        var tab = localStorage.getItem("tab");
        if (tab) {
            $('section').hide(); // Hide all sections
            $(tab).show(); // Show the saved tab
            $('.link-header').removeClass('active'); // Remove 'active' class from all links
            $('[href="' + tab + '"]').addClass('active'); // Add 'active' class to the corresponding link
        } else {
            // If no tab is saved, show the default tab
            $('#staff').show();
            $('[href="#staff"]').addClass('active');
        }
    }
   
    /* END  NAVBAR    */
   

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