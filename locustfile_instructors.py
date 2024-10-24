from datetime import datetime, timedelta
from locust import HttpUser, task, constant_pacing
import random
import threading
from faker import Faker

fake = Faker()
instructors = []
instructors_lock = threading.Lock()


class InstructorManagerUser(HttpUser):
    wait_time = constant_pacing(1)

    def on_start(self):
        global instructors
        self.init_instructors()
            
    def init_instructors(self):
        response = self.client.get("http://localhost:5003/instructors")
        if response.status_code == 200:
            response_data = response.json()
            instructors = [
                instructor["id"]
                for instructor in response_data
            ]
            print("Instructor IDs initialized:", len(instructors))
        else:
            print("Failed to fetch instructors. Status code:", response.status_code)
            
    def get_random_instructor(self):
        if instructors:
            random_index = random.randint(0, len(instructors) - 1)
            random_instructor = instructors[random_index]
            return random_instructor
        else:
            return None

    @task
    def get_instructor(self):
        response = self.client.get("/instructors")
        print(f"GET(instructors). Status code: {response.status_code}")
        

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

        if response.status_code != 201 and response.content:
            return
        
        response_data = response.json()
        with instructors_lock:
            instructors.append(response_data["id"])

    @task
    def get_instructor_by_id(self):
        random_instructor = self.get_random_instructor()
        response = self.client.get(f"/instructors/{random_instructor}")
        print(f"GET(instructors/id). Status code: {response.status_code}")
            
    @task
    def put_course(self):
        body = {
            "firstName": fake.first_name(),
            "lastName": fake.last_name(),
            "email": fake.email(),
            "phone": fake.phone_number(),
            "birthDate": (datetime.now() + timedelta(days=random.randint(1, 30))).strftime("%Y-%m-%d")
        }
        
        random_instructor = self.get_random_instructor()
        response = self.client.put(f"/instructors/{random_instructor}", json=body)
        
        print(f"PUT(instructors). Status code: {response.status_code} {response.text}")

    @task
    def delete_course(self):
        random_instructor = self.get_random_instructor()
        
        response = self.client.delete(f"/instructors/{random_instructor}")
        print(f"DELETE(instructors). Status code: {response.status_code} {response.text}")
        if response.status_code != 204: 
            return
        
        with instructors_lock:
            instructors.remove(random_instructor)