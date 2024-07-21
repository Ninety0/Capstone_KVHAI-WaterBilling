$(document).ready(function () {
    let st_id = 0;

    $('#btn-streets').click(handleRegistration);
    $('#btn-update').click(UpdateStreet);

    function validate() {
        const form = $('#form-street'); // jQuery
        const inputs = form.find('input'); // jQuery

        let isValid = true;

        inputs.each(function () {
            const input = $(this); // Store jQuery object reference for `this`

            // Handle other inputs
            if (!this.checkValidity()) {
                isValid = false;
                input.addClass('is-invalid');
            } else {
                input.removeClass('is-invalid');
            }
        });

        return isValid;
    }

    //INSERT
    function handleRegistration(e) {
        e.preventDefault();

        if (!validate()) {
            toastr.error('Please fill out all required fields correctly.');
            return;
        }

        //var formData = new FormData($('#form-emp')[0]);
        var formData = $('#form-street').serialize();
        console.log(formData);

        $.ajax({
            type: 'POST',
            url: "/Street/RegisterStreet",
            contentType: 'application/x-www-form-urlencoded; charset=UTF-8', // when we use .serialize() this generates the data in query string format. this needs the default contentType (default content type is: contentType: 'application/x-www-form-urlencoded; charset=UTF-8') so it is optional, you can remove it
            data: formData,
            success: function (response) {
                if (response?.message?.includes('exist')) {
                    toastr.error('Email or Username already taken.');
                    return;
                }

                toastr.success('Registration Successful.');
                // Update your table here if needed
                $('#modal-street').modal('hide');
                $(".modal-backdrop").remove();

                var result = $(response).find("#tableData").html();
                console.log("RESPONSE WHEN SUCCESS");
                console.log(result);
                $('#tableData').html(result);


            },
            error: function (xhr, status, error) {
                console.log(xhr.responseText);
                toastr.error('An error occurred while processing your request.');
            }
        });

        $('#modal-employee').on('hidden.bs.modal', function () {
            // Reset modal content when it's closed
            $('#form-emp')[0].reset();
        });
    }

    //UPDATE
    function UpdateStreet(e) {
        e.preventDefault();

        if (!validate("update")) {
            toastr.error('Please fill out all required fields correctly.');
            return;
        }

        // Gather form data using FormData
        var formData = new FormData($('#form-street')[0]);
        formData.append('Street_ID', st_id);

        // Serialize form data into JSON format (assuming you want JSON)

        $.ajax({
            type: 'POST',
            url: "/street/UpdateStreet",
            contentType: false,
            processData: false,
            data: formData,
            success: function (response) {
                if (response?.message?.includes('error')) {
                    toastr.error('There was an error updating the street.');
                    return;
                }

                toastr.success('Update Successfully.');
                // Update your table here if needed

                $('#modal-street').modal('hide');
                $(".modal-backdrop").remove();

                // Assuming the response contains HTML table data:
                var tableData = $(response).find("#tableData").html();
                console.log("RESPONSE WHEN SUCCESS");
                console.log(tableData);
                $('#tableData').html(tableData);
            },
            error: function (xhr, status, error) {
                console.log(xhr.responseText);
                toastr.error('An error occurred while processing your request.');
            }
        });

        $('#modal-employee').on('hidden.bs.modal', function () {
            // Reset modal content when it's closed
            $('#form-emp')[0].reset();
            $('#btn-update').addClass('d-none');
            $('#btn-streets').removeClass('d-none');
        });
    }

    //GET METHOD TO FETCH DATA
    $(document).on('click', '.edit-btn', function () {
        $('#btn-update').removeClass('d-none');
        $('#btn-streets').addClass('d-none');

        st_id = $(this).data('id');
        //alert('Edit button clicked for Employee ID:' + emp_id);
        var arr = {
            id: st_id
        }
        $.ajax({
            type: 'GET',
            url: "/Street/GetStreet",
            data: arr,
            success: function (response) {
                console.log(response);
                if (response.length > 0) {
                    var emp = response[0]; // Access the first element of the array

                    $('#employeeId').val(emp.street_ID);
                    $('#St_Name').val(emp.street_Name);
                    $('#modal-street').modal('show');

                }
                else {
                    toastr.error('Failed to load employee data.');
                }
            },
            error: function (xhr, status, error) {
                toastr.error('Failed to load employee data.');
            }
        });

        $('#modal-street').on('hidden.bs.modal', function () {
            // Reset modal content when it's closed
            $('#form-street')[0].reset();
            $('#btn-update').addClass('d-none');
            $('#btn-streets').removeClass('d-none');
        });
    });

    //METHOD TO DELETE
    $(document).on('click', '.delete-btn', function () {

        st_id = $(this).data('id');
        alert('Edit button clicked for Employee ID:' + st_id);
        var arr = {
            id: st_id
        }
        $.ajax({
            type: 'DELETE',
            url: '/street/DeleteStreet',
            data: arr,
            success: function (response) {
                console.log(response);
                if (response?.message?.includes('error')) {
                    toastr.error('There was an error deleting the account.');
                    return;
                }

                toastr.success('Delete Successfully.');

                var result = $(response).find("#tableData").html();
                console.log("RESPONSE WHEN SUCCESS");
                console.log(result);
                $('#tableData').html(result);
            },
            error: function (xhr, status, error) {
                toastr.error('An error occurred while processing your request.');
            }
        });

    });


});
//    < !--PAGINATION -->
$(document).on('change', '#st-search', function () {
    pagination();
})

function pagination(i = 1) {//Yung i is default pero pwedeng ibang letter ilagay dyan [i] lang nilalagay ko
    var _search = $('#st-search').val();

    var array = {
        search: _search,
        page_index: i
    };

    $.ajax({
        url: window.location.origin + '/street/Pagination',
        type: "POST",
        data: array,
        success: function (response) {
            var result = $(response).find("#tableData").html();
            console.log(result);
            $('#tableData').html(result)
        },
        error: function (xhr, status, error_m) {
            alert(status);
        }
    });
}
