$(document).ready(function () {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))
    //Global Variable
    let emp_id = 0;

    $('#btn-test').on('click', () => {
        const form = $('#form-emp');
        const inputs = form.find('input, select');
        inputs.each(function () {
            console.log(this.id);
        });
    });

    // Toastr options
    toastr.options = {
        closeButton: true,
        progressBar: true,
        positionClass: 'toast-top-right',
        timeOut: 5000
    };

    // Event listeners
    $('#cpass').on('change', ConfirmPassword);
    $('#btn-register').click(handleRegistration);
    $('#btn-update').click(UpdateEmployee);


    function validateCurrentTab(action) {
        const form = $('#form-emp'); // jQuery
        const inputs = form.find('input, select'); // jQuery

        let isValid = true;

        inputs.each(function () {
            const input = $(this); // Store jQuery object reference for `this`

            if (action === "update") {
                if (this.id === 'cpass') {
                    // Handle confirm password separately
                    if (!ConfirmPassword()) {
                        isValid = false;
                        input.addClass('is-invalid');
                    } else {
                        input.removeClass('is-invalid');
                    }
                } else if (this.id === 'Password') {
                    if ($('#Password').val() === '') {
                        input.removeClass('is-invalid');
                        isValid = true;
                    }
                } else {
                    // Handle other inputs
                    if (!this.checkValidity()) {
                        isValid = false;
                        input.addClass('is-invalid');
                    } else {
                        input.removeClass('is-invalid');
                    }
                }
            } else {
                if (this.id === 'cpass') {
                    // Handle confirm password separately
                    if (!ConfirmPassword()) {
                        isValid = false;
                        input.addClass('is-invalid');
                    } else {
                        input.removeClass('is-invalid');
                    }
                } else {
                    // Handle other inputs
                    if (!this.checkValidity()) {
                        isValid = false;
                        input.addClass('is-invalid');
                    } else {
                        input.removeClass('is-invalid');
                    }
                }
            }
        });

        return isValid;
    }

    


    function ConfirmPassword() {
        

        const pass = document.getElementById('Password').value;
        const cpass = document.getElementById('cpass');
        const errorMsg = document.getElementById('err-msg');

        const isValid = pass === cpass.value;

        cpass.classList.toggle('is-invalid', !isValid);
        errorMsg.style.display = isValid ? 'none' : 'block';

        return isValid;
    }

    //POST METHOD TO INSERT NEW EMPLOYEE 
    function handleRegistration(e) {
        e.preventDefault();

        if (!validateCurrentTab("register")) {
            toastr.error('Please fill out all required fields correctly.');
            return;
        }

        //var formData = new FormData($('#form-emp')[0]);
        var formData = $('#form-emp').serialize();
        console.log(formData);

        $.ajax({
            type: 'POST',
            url: "/adminaccount/RegisterEmployee",
            contentType: 'application/x-www-form-urlencoded; charset=UTF-8', // when we use .serialize() this generates the data in query string format. this needs the default contentType (default content type is: contentType: 'application/x-www-form-urlencoded; charset=UTF-8') so it is optional, you can remove it
            data: formData,
            success: function (response) {
                if (response?.message?.includes('exist')) {
                    toastr.error('Email or Username already taken.');
                    return;
                }

                toastr.success('Registration Successful.');
                // Update your table here if needed
                $('#modal-employee').modal('hide');
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

    // Remove 'is-invalid' class on input
    $('input, select').on('input', function () {
        $(this).removeClass('is-invalid');
        $(this).closest('form').removeClass('was-validated');
    });


    //POST METHOD TO UPDATE EMPLOYEE
    function UpdateEmployee(e) {
        e.preventDefault();

        if (!validateCurrentTab("update")) {
            toastr.error('Please fill out all required fields correctly.');
            return;
        }

        // Gather form data using FormData
        var formData = new FormData($('#form-emp')[0]);
        formData.append('Emp_ID', emp_id);

        // Serialize form data into JSON format (assuming you want JSON)

        $.ajax({
            type: 'POST',
            url: "/adminaccount/UpdateEmployee",
            contentType: false,
            processData: false,
            data: formData,
            success: function (response) {
                if (response?.message?.includes('error')) {
                    toastr.error('There was an error updating credentials.');
                    return;
                }

                toastr.success('Update Account Successfully.');
                // Update your table here if needed

                $('#modal-employee').modal('hide');
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
            $('#btn-register').removeClass('d-none');
        });
    }

    //POST METHOD TO DELETE EMPLOYEE
    $(document).on('click', '.delete-btn', function () {

        emp_id = $(this).data('id');
        alert('Edit button clicked for Employee ID:' + emp_id);
        var arr = {
            id: emp_id
        }
        $.ajax({
            type: 'DELETE',
            url: '/adminaccount/DeleteEmployee',
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

    //GET METHOD TO FETCH DATA
    $(document).on('click', '.edit-btn', function () {
        $('#btn-update').removeClass('d-none');
        $('#btn-register').addClass('d-none');

        emp_id = $(this).data('id');
        //alert('Edit button clicked for Employee ID:' + emp_id);
        var arr = {
            id: emp_id
        }
        $.ajax({
            type: 'GET',
            url: '/adminaccount/GetEmployees',
            data: arr,
            success: function (response) {
                console.log(response);
                if (response.length > 0) {
                    var emp = response[0]; // Access the first element of the array

                    $('#employeeId').val(emp.emp_ID);
                    $('#Fname').val(emp.fname);
                    $('#Lname').val(emp.lname);
                    $('#Mname').val(emp.mname);
                    $('#Phone').val(emp.phone);
                    $('#Email').val(emp.email);
                    $('#Username').val(emp.username);
                    $('#Role').val(emp.role);
                    $('#modal-employee').modal('show');

                }
                else {
                    toastr.error('Failed to load employee data.');
                }
            },
            error: function (xhr, status, error) {
                toastr.error('Failed to load employee data.');
            }
        });

        $('#modal-employee').on('hidden.bs.modal', function () {
            // Reset modal content when it's closed
            $('#form-emp')[0].reset();
        });
    });
});
