from datetime import datetime, timedelta
from locust import HttpUser, task, constant_pacing
import random
import threading
from faker import Faker

fake = Faker()
students = []
students_lock = threading.Lock()

class StudentManagerUser(HttpUser):
    wait_time = constant_pacing(1)

    def on_start(self):
        global students
        self.init_students()

    def init_students(self):
        response = self.client.get("http://localhost:5002/students")
        if response.status_code == 200:
            response_data = response.json()
            students = [
                student["id"]
                for student in response_data
            ]
            print("Student IDs initialized:", len(students))
        else:
            print("Failed to fetch students. Status code:", response.status_code)

    def get_random_student(self):
        if students:
            random_index = random.randint(0, len(students) - 1)
            random_student = students[random_index]
            return random_student
        else:
            return None

    @task
    def get_students(self):
        response = self.client.get("/students")
        print(f"GET(students). Status code: {response.status_code}")

    @task
    def create_student(self):
        body = {
            "firstName": fake.first_name(),
            "lastName": fake.last_name(),
            "email": fake.email(),
            "phone": fake.phone_number(),
            "birthDate": (datetime.now() + timedelta(days=random.randint(1, 30))).strftime("%Y-%m-%d"),
        }

        response = self.client.post("/students", json=body)
        print(f"POST(students). Status code: {response.status_code}.")

        if response.status_code != 201 and response.content:
            return
        
        response_data = response.json()
        with students_lock:
            students.append(response_data["id"])

    @task
    def get_student_by_id(self):
        random_student = self.get_random_student()
        if random_student:
            response = self.client.get(f"/students/{random_student}")
            print(f"GET(students/id). Status code: {response.status_code}")

    @task
    def update_student(self):
        body = {
            "firstName": fake.first_name(),
            "lastName": fake.last_name(),
            "email": fake.email(),
            "phone": fake.phone_number(),
            "birthDate": (datetime.now() + timedelta(days=random.randint(1, 30))).strftime("%Y-%m-%d"),
        }

        random_student = self.get_random_student()
        if random_student:
            response = self.client.put(f"/students/{random_student}", json=body)
            print(f"PUT(students). Status code: {response.status_code} {response.text}")

    @task
    def delete_student(self):
        random_student = self.get_random_student()
        if random_student:
            response = self.client.delete(f"/students/{random_student}")
            print(f"DELETE(students). Status code: {response.status_code} {response.text}")
            if response.status_code == 204:
                with students_lock:
                    students.remove(random_student)
