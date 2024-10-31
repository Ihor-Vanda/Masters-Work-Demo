from datetime import datetime, timedelta
from locust import HttpUser, task, constant_pacing
import random
from faker import Faker

fake = Faker()

class CourseManagerUser(HttpUser):
    host = "http://localhost:5001"
    wait_time = constant_pacing(1)
        
    def generate_course_body(self, course_id=None):
        course_code = f"COURSE-{random.randint(1000, 9999)}"
        title = f"Course Title {random.randint(1, 100)}"
        description = "This is a description of the course."
        language = "English"
        status = "Active"
        start_date = (datetime.now() + timedelta(days=random.randint(1, 30))).strftime("%Y-%m-%d")
        end_date = (datetime.now() + timedelta(days=random.randint(31, 60))).strftime("%Y-%m-%d")

        if course_id:
            current_students = 0
            max_students = 0
            
            for course in courses:
                if course["courseId"] == course_id:
                    current_students = len(course["studentIds"])
                    max_students = course["maxStudents"]
                    break
            max_students = max(current_students, max_students + random.randint(0, max_students))
                
        else:
            max_students = random.randint(1, 50)

        body = {
            "courseCode": course_code,
            "title": title,
            "description": description,
            "language": language,
            "status": status,
            "startDate": start_date,
            "endDate": end_date, 
            "maxStudents": max_students
        }
        return body
            
    @task
    def create_course(self):
        body = self.generate_course_body()

        response = self.client.post("/courses", json=body) 

        if response.status_code != 201 and response.content:
            return

        print(f"POST(courses). Status code: {response.status_code}")
        

class StudentManagerUser(HttpUser):
    host = "http://localhost:5002"
    wait_time = constant_pacing(1)

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


class InstructorManagerUser(HttpUser):
    host = "http://localhost:5003"
    wait_time = constant_pacing(1)

    @task
    def create_instructor(self):
        body = {
            "firstName": fake.first_name(),
            "lastName": fake.last_name(),
            "email": fake.email(),
            "phone": fake.phone_number(),
            "birthDate": (datetime.now() + timedelta(days=random.randint(1, 30))).strftime("%Y-%m-%d")
        }

        response = self.client.post("/instructors", json=body) 
        print(f"POST(instructors). Status code: {response.status_code}.")